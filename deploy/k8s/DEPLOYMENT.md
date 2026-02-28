# Orleans Silo Kubernetes Deployment

This updates the API deployment to run Orleans with Kubernetes-native clustering instead of localhost-only mode. After applying, the API can run as multiple replicas with distributed grain placement.

## What Changed (Code)

The API image (`ghcr.io/andyjmorgan/donkeywork-agents/api:latest`) now detects the environment at startup via `HostBuilderContext.HostingEnvironment.IsProduction()`:

- **Production**: Uses `UseKubernetesHosting()` (reads ClusterId/ServiceId from pod labels, sets silo name to pod name) + `UseKubeMembership()` (cluster membership via K8s API using ConfigMaps).
- **Development**: Falls back to `UseLocalhostClustering(serviceId: "donkeywork-agents", clusterId: "donkeywork-agents")`. ClusterId/ServiceId are passed directly as parameters because `UseLocalhostClustering` uses `PostConfigure<ClusterOptions>` internally which would override a separate `Configure<ClusterOptions>` call.

CI/CD builds the image automatically on push to main. No manual image build required.

## What Needs to Happen (Infrastructure)

Four manifests in `deploy/k8s/` need to be applied **in order**. All target namespace `donkeywork-agents`.

### 1. SeaweedFS Service Patch

**File:** `seaweedfs-service-patch.yaml`

**Why:** Orleans grain state is stored in SeaweedFS via its Filer HTTP API on port 8888. The current SeaweedFS service only exposes port 8333 (S3). The API pod's `Actors__SeaweedFsBaseUrl` is set to `http://seaweedfs:8888`, so the filer port must be reachable.

**What it does:** Adds port 8888 (filer) alongside the existing 8333 (s3) on the `seaweedfs` ClusterIP service.

**Prerequisite:** Confirm the SeaweedFS pod actually has filer running on 8888. The docker-compose config runs `server -s3 -dir=/data` which starts master + filer + S3 on the same container. If the K8s SeaweedFS pod uses the same command, filer is already listening. If it runs a different entrypoint (e.g. volume server only), this port will have nothing behind it.

```bash
# Verify filer is running inside the SeaweedFS pod
kubectl -n donkeywork-agents exec deploy/seaweedfs -- curl -sf http://localhost:8888/
```

```bash
kubectl apply -f deploy/k8s/seaweedfs-service-patch.yaml
```

### 2. RBAC (ServiceAccount + Role + RoleBinding)

**File:** `api-rbac.yaml`

**Why:** Orleans K8s clustering needs API access to discover peer pods and maintain membership state. Without this, the silo will fail to start with 403 errors.

**What it creates:**
| Resource | Name | Purpose |
|----------|------|---------|
| ServiceAccount | `api-silo` | Identity for the API pods |
| Role | `api-silo` | Scoped to `donkeywork-agents` namespace |
| RoleBinding | `api-silo` | Binds the role to the service account |

**Permissions granted:**

| Resource | Verbs | Used For |
|----------|-------|----------|
| `pods` | get, watch, list, patch, delete | Pod discovery, silo lifecycle |
| `configmaps` | get, watch, list, create, update, patch | Cluster membership table |
| `leases` | get, watch, list, create, update, patch | Leader election |

```bash
kubectl apply -f deploy/k8s/api-rbac.yaml
```

### 3. API Service Update

**File:** `api-service.yaml`

**Why:** Orleans silos communicate over two additional ports. Without exposing these, pods can't discover or route to each other.

**What changes from current:**
| Port | Name | Purpose | Status |
|------|------|---------|--------|
| 8080 | http | HTTP API + WebSocket | Existing |
| 11111 | orleans-silo | Silo-to-silo communication | **New** |
| 30000 | orleans-gateway | Client-to-silo gateway | **New** |

```bash
kubectl apply -f deploy/k8s/api-service.yaml
```

### 4. API Deployment Update

**File:** `api-deployment.yaml`

**Image:** `ghcr.io/andyjmorgan/donkeywork-agents/api:latest`

**Key differences from current deployment:**

| Setting | Before | After |
|---------|--------|-------|
| Replicas | 1 | 2 |
| ServiceAccount | (default) | `api-silo` |
| Container ports | 8080 | 8080, 11111, 30000 |
| Pod labels | `app: api` | `app: api`, `orleans/serviceId: donkeywork-agents`, `orleans/clusterId: donkeywork-agents` |
| Termination grace period | (default 30s) | 180s |
| Rolling update strategy | 25%/25% | maxUnavailable: 0, maxSurge: 1 |
| Memory request | 128Mi | 256Mi |
| Memory limit | 512Mi | 1Gi |
| CPU limit | 500m | 1 |

**New env vars:**

| Variable | Value | Purpose |
|----------|-------|---------|
| `POD_NAME` | `fieldRef: metadata.name` | Orleans silo identity |
| `POD_NAMESPACE` | `fieldRef: metadata.namespace` | K8s API namespace scoping |
| `POD_IP` | `fieldRef: status.podIP` | Silo endpoint advertisement |
| `Actors__SeaweedFsBaseUrl` | `http://seaweedfs:8888` | Grain state storage (was defaulting to `http://localhost:8888`) |

All existing env vars are preserved unchanged (Persistence, Keycloak, Storage, RabbitMQ, Agents).

```bash
kubectl apply -f deploy/k8s/api-deployment.yaml
```

## Full Apply Sequence

```bash
kubectl apply -f deploy/k8s/seaweedfs-service-patch.yaml
kubectl apply -f deploy/k8s/api-rbac.yaml
kubectl apply -f deploy/k8s/api-service.yaml
kubectl apply -f deploy/k8s/api-deployment.yaml
```

Or all at once:

```bash
kubectl apply -f deploy/k8s/
```

## Verification

```bash
# Watch pods come up (expect 2 replicas)
kubectl -n donkeywork-agents get pods -l app=api -w

# Check Orleans cluster formation in logs (look for "Silo is active")
kubectl -n donkeywork-agents logs -l app=api --tail=50 | grep -i "silo\|orleans\|cluster"

# Verify SeaweedFS filer is reachable from an API pod
kubectl -n donkeywork-agents exec deploy/api -- curl -sf http://seaweedfs:8888/

# Verify RBAC is working (no 403s in logs)
kubectl -n donkeywork-agents logs -l app=api --tail=100 | grep -i "forbidden\|403\|unauthorized"
```

## Rollback

If something goes wrong, revert to the previous single-replica deployment:

```bash
# Scale back to 1 and remove Orleans labels
kubectl -n donkeywork-agents scale deploy/api --replicas=1
```

The previous deployment manifest (without Orleans labels/ports/RBAC) can be reapplied from the `kubectl.kubernetes.io/last-applied-configuration` annotation on the existing deployment if needed.

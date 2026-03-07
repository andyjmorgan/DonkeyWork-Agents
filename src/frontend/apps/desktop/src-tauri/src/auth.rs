use base64::{engine::general_purpose::URL_SAFE_NO_PAD, Engine};
use rand::Rng;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::sync::Arc;
use tauri::{AppHandle, Emitter};
use tauri_plugin_opener::OpenerExt;
use tauri_plugin_store::StoreExt;
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::sync::Mutex;

const STORE_FILENAME: &str = "auth.json";
const KEYCLOAK_AUTHORITY: &str =
    "https://auth.donkeywork.dev/realms/Agents";
const KEYCLOAK_CLIENT_ID: &str = "donkeywork-agents-api";
const API_BASE_URL: &str = "https://agents.donkeywork.dev";

// Store keys
const KEY_ACCESS_TOKEN: &str = "access_token";
const KEY_REFRESH_TOKEN: &str = "refresh_token";
const KEY_EXPIRES_AT: &str = "expires_at";
const KEY_TOKEN_ISSUED_AT: &str = "token_issued_at";

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AuthTokens {
    pub access_token: String,
    pub refresh_token: Option<String>,
    pub expires_in: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct StoredTokens {
    pub access_token: String,
    pub refresh_token: Option<String>,
    pub expires_at: i64,
    pub token_issued_at: i64,
}

/// Keycloak token endpoint response
#[derive(Debug, Deserialize)]
struct KeycloakTokenResponse {
    access_token: String,
    refresh_token: Option<String>,
    expires_in: i64,
}

/// Refresh response from our backend
#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct BackendRefreshResponse {
    access_token: String,
    refresh_token: Option<String>,
    expires_in: i64,
}

/// PKCE state held in memory during auth flow
pub struct PkceState {
    code_verifier: Option<String>,
}

impl PkceState {
    pub fn new() -> Self {
        Self {
            code_verifier: None,
        }
    }
}

pub type SharedPkceState = Arc<Mutex<PkceState>>;

fn generate_code_verifier() -> String {
    let mut bytes = [0u8; 32];
    rand::rng().fill(&mut bytes);
    URL_SAFE_NO_PAD.encode(bytes)
}

fn generate_code_challenge(verifier: &str) -> String {
    let mut hasher = Sha256::new();
    hasher.update(verifier.as_bytes());
    let hash = hasher.finalize();
    URL_SAFE_NO_PAD.encode(hash)
}

fn now_millis() -> i64 {
    std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_millis() as i64
}

/// Start the OAuth PKCE flow using a localhost callback server.
/// Opens the system browser, waits for the callback, exchanges the code,
/// and returns the tokens directly.
#[tauri::command]
pub async fn start_auth(
    app: AppHandle,
    pkce_state: tauri::State<'_, SharedPkceState>,
) -> Result<AuthTokens, String> {
    let code_verifier = generate_code_verifier();
    let code_challenge = generate_code_challenge(&code_verifier);

    // Bind to a random available port
    let listener = tokio::net::TcpListener::bind("127.0.0.1:0")
        .await
        .map_err(|e| format!("Failed to bind callback server: {}", e))?;

    let port = listener
        .local_addr()
        .map_err(|e| format!("Failed to get local address: {}", e))?
        .port();

    let redirect_uri = format!("http://localhost:{}/auth/callback", port);

    let auth_url = format!(
        "{}/protocol/openid-connect/auth?\
         client_id={}\
         &response_type=code\
         &scope={}\
         &redirect_uri={}\
         &code_challenge={}\
         &code_challenge_method=S256\
         &kc_idp_hint=github",
        KEYCLOAK_AUTHORITY,
        urlencoding::encode(KEYCLOAK_CLIENT_ID),
        urlencoding::encode("openid profile email"),
        urlencoding::encode(&redirect_uri),
        urlencoding::encode(&code_challenge),
    );

    // Open system browser
    app.opener()
        .open_url(&auth_url, None::<&str>)
        .map_err(|e| e.to_string())?;

    // Wait for the callback (with timeout)
    let code = tokio::time::timeout(
        tokio::time::Duration::from_secs(300),
        wait_for_callback(listener),
    )
    .await
    .map_err(|_| "Login timed out after 5 minutes".to_string())?
    .map_err(|e| format!("Callback failed: {}", e))?;

    // Exchange code for tokens
    let token_endpoint = format!(
        "{}/protocol/openid-connect/token",
        KEYCLOAK_AUTHORITY
    );

    let params = [
        ("grant_type", "authorization_code"),
        ("client_id", KEYCLOAK_CLIENT_ID),
        ("code", code.as_str()),
        ("redirect_uri", redirect_uri.as_str()),
        ("code_verifier", code_verifier.as_str()),
    ];

    let client = reqwest::Client::new();
    let response = client
        .post(&token_endpoint)
        .form(&params)
        .send()
        .await
        .map_err(|e| format!("Token exchange request failed: {}", e))?;

    if !response.status().is_success() {
        let error_text = response.text().await.unwrap_or_default();
        return Err(format!("Token exchange failed: {}", error_text));
    }

    let token_response: KeycloakTokenResponse = response
        .json()
        .await
        .map_err(|e| format!("Failed to parse token response: {}", e))?;

    let tokens = AuthTokens {
        access_token: token_response.access_token,
        refresh_token: token_response.refresh_token,
        expires_in: token_response.expires_in,
    };

    // Store tokens
    store_tokens_internal(&app, &tokens)?;

    Ok(tokens)
}

/// Wait for the OAuth callback on the local TCP listener.
/// Parses the authorization code from the query string and sends a success page.
async fn wait_for_callback(listener: tokio::net::TcpListener) -> Result<String, String> {
    let (mut stream, _) = listener
        .accept()
        .await
        .map_err(|e| format!("Failed to accept connection: {}", e))?;

    let mut buf = vec![0u8; 4096];
    let n = stream
        .read(&mut buf)
        .await
        .map_err(|e| format!("Failed to read request: {}", e))?;

    let request = String::from_utf8_lossy(&buf[..n]);

    // Parse the GET request line to extract the path
    let path = request
        .lines()
        .next()
        .and_then(|line| line.split_whitespace().nth(1))
        .ok_or_else(|| "Invalid HTTP request".to_string())?;

    // Parse query parameters from the path
    let url = format!("http://localhost{}", path);
    let parsed = reqwest::Url::parse(&url)
        .map_err(|e| format!("Failed to parse callback URL: {}", e))?;

    // Check for errors
    if let Some(error) = parsed.query_pairs().find(|(k, _)| k == "error") {
        let error_desc = parsed
            .query_pairs()
            .find(|(k, _)| k == "error_description")
            .map(|(_, v)| v.to_string())
            .unwrap_or_default();

        let error_page = format!(
            "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nConnection: close\r\n\r\n\
            <html><body style=\"font-family:system-ui;text-align:center;padding:60px\">\
            <h2>Authentication Failed</h2><p>{}: {}</p>\
            <p>You can close this tab.</p></body></html>",
            error.1, error_desc
        );
        let _ = stream.write_all(error_page.as_bytes()).await;
        let _ = stream.shutdown().await;
        return Err(format!("Auth error: {}", error.1));
    }

    let code = parsed
        .query_pairs()
        .find(|(k, _)| k == "code")
        .map(|(_, v)| v.to_string())
        .ok_or_else(|| "No authorization code in callback".to_string())?;

    // Send success response and close
    let success_page = "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nConnection: close\r\n\r\n\
        <html><body style=\"font-family:system-ui;text-align:center;padding:60px\">\
        <h2>Login Successful!</h2>\
        <p>You can close this tab and return to DonkeyWork.</p>\
        <script>window.close()</script></body></html>";

    let _ = stream.write_all(success_page.as_bytes()).await;
    let _ = stream.shutdown().await;

    Ok(code)
}

/// Store tokens securely using tauri-plugin-store
fn store_tokens_internal(app: &AppHandle, tokens: &AuthTokens) -> Result<(), String> {
    let store = app
        .store(STORE_FILENAME)
        .map_err(|e| format!("Failed to open store: {}", e))?;

    let now = now_millis();
    let expires_at = now + tokens.expires_in * 1000;

    store.set(
        KEY_ACCESS_TOKEN,
        serde_json::Value::String(tokens.access_token.clone()),
    );
    if let Some(ref rt) = tokens.refresh_token {
        store.set(
            KEY_REFRESH_TOKEN,
            serde_json::Value::String(rt.clone()),
        );
    }
    store.set(
        KEY_EXPIRES_AT,
        serde_json::Value::Number(serde_json::Number::from(expires_at)),
    );
    store.set(
        KEY_TOKEN_ISSUED_AT,
        serde_json::Value::Number(serde_json::Number::from(now)),
    );

    store.save().map_err(|e| format!("Failed to save store: {}", e))?;

    Ok(())
}

/// Get stored tokens
#[tauri::command]
pub async fn get_tokens(app: AppHandle) -> Result<Option<StoredTokens>, String> {
    let store = app
        .store(STORE_FILENAME)
        .map_err(|e| format!("Failed to open store: {}", e))?;

    let access_token = store
        .get(KEY_ACCESS_TOKEN)
        .and_then(|v| v.as_str().map(String::from));

    let access_token = match access_token {
        Some(t) => t,
        None => return Ok(None),
    };

    let refresh_token = store
        .get(KEY_REFRESH_TOKEN)
        .and_then(|v| v.as_str().map(String::from));

    let expires_at = store
        .get(KEY_EXPIRES_AT)
        .and_then(|v| v.as_i64())
        .unwrap_or(0);

    let token_issued_at = store
        .get(KEY_TOKEN_ISSUED_AT)
        .and_then(|v| v.as_i64())
        .unwrap_or(0);

    Ok(Some(StoredTokens {
        access_token,
        refresh_token,
        expires_at,
        token_issued_at,
    }))
}

/// Clear stored tokens
#[tauri::command]
pub async fn clear_tokens(app: AppHandle) -> Result<(), String> {
    let store = app
        .store(STORE_FILENAME)
        .map_err(|e| format!("Failed to open store: {}", e))?;

    store.delete(KEY_ACCESS_TOKEN);
    store.delete(KEY_REFRESH_TOKEN);
    store.delete(KEY_EXPIRES_AT);
    store.delete(KEY_TOKEN_ISSUED_AT);
    store.save().map_err(|e| format!("Failed to save store: {}", e))?;

    Ok(())
}

/// Refresh tokens via the backend endpoint
#[tauri::command]
pub async fn refresh_tokens(app: AppHandle) -> Result<AuthTokens, String> {
    let store = app
        .store(STORE_FILENAME)
        .map_err(|e| format!("Failed to open store: {}", e))?;

    let refresh_token = store
        .get(KEY_REFRESH_TOKEN)
        .and_then(|v| v.as_str().map(String::from))
        .ok_or_else(|| "No refresh token available".to_string())?;

    let client = reqwest::Client::new();
    let response = client
        .post(format!("{}/api/v1/auth/refresh", API_BASE_URL))
        .json(&serde_json::json!({ "refreshToken": refresh_token }))
        .send()
        .await
        .map_err(|e| format!("Refresh request failed: {}", e))?;

    if !response.status().is_success() {
        let error_text = response.text().await.unwrap_or_default();
        return Err(format!("Token refresh failed: {}", error_text));
    }

    let refresh_response: BackendRefreshResponse = response
        .json()
        .await
        .map_err(|e| format!("Failed to parse refresh response: {}", e))?;

    let tokens = AuthTokens {
        access_token: refresh_response.access_token,
        refresh_token: refresh_response.refresh_token,
        expires_in: refresh_response.expires_in,
    };

    // Store new tokens
    store_tokens_internal(&app, &tokens)?;

    Ok(tokens)
}

/// Start background token refresh loop
pub fn start_token_refresh_loop(app: AppHandle) {
    tauri::async_runtime::spawn(async move {
        loop {
            tokio::time::sleep(tokio::time::Duration::from_secs(60)).await;

            let should_refresh = {
                let store = match app.store(STORE_FILENAME) {
                    Ok(s) => s,
                    Err(_) => continue,
                };

                let expires_at = store
                    .get(KEY_EXPIRES_AT)
                    .and_then(|v| v.as_i64())
                    .unwrap_or(0);

                let token_issued_at = store
                    .get(KEY_TOKEN_ISSUED_AT)
                    .and_then(|v| v.as_i64())
                    .unwrap_or(0);

                let refresh_token = store
                    .get(KEY_REFRESH_TOKEN)
                    .and_then(|v| v.as_str().map(String::from));

                if refresh_token.is_none() || expires_at == 0 {
                    false
                } else {
                    let now = now_millis();
                    let token_lifetime = expires_at - token_issued_at;
                    let time_remaining = expires_at - now;
                    // Refresh when 20% of lifetime remains (80% elapsed)
                    let threshold = (token_lifetime as f64 * 0.2) as i64;
                    time_remaining <= threshold && time_remaining > 0
                }
            };

            if should_refresh {
                match refresh_tokens_internal(&app).await {
                    Ok(tokens) => {
                        let _ = app.emit("tokens-refreshed", &tokens);
                    }
                    Err(e) => {
                        eprintln!("Background token refresh failed: {}", e);
                        let _ = app.emit("auth-expired", ());
                    }
                }
            }
        }
    });
}

/// Internal refresh (not a tauri command)
async fn refresh_tokens_internal(app: &AppHandle) -> Result<AuthTokens, String> {
    let store = app
        .store(STORE_FILENAME)
        .map_err(|e| format!("Failed to open store: {}", e))?;

    let refresh_token = store
        .get(KEY_REFRESH_TOKEN)
        .and_then(|v| v.as_str().map(String::from))
        .ok_or_else(|| "No refresh token available".to_string())?;

    let client = reqwest::Client::new();
    let response = client
        .post(format!("{}/api/v1/auth/refresh", API_BASE_URL))
        .json(&serde_json::json!({ "refreshToken": refresh_token }))
        .send()
        .await
        .map_err(|e| format!("Refresh request failed: {}", e))?;

    if !response.status().is_success() {
        let error_text = response.text().await.unwrap_or_default();
        return Err(format!("Token refresh failed: {}", error_text));
    }

    let refresh_response: BackendRefreshResponse = response
        .json()
        .await
        .map_err(|e| format!("Failed to parse refresh response: {}", e))?;

    let tokens = AuthTokens {
        access_token: refresh_response.access_token,
        refresh_token: refresh_response.refresh_token,
        expires_in: refresh_response.expires_in,
    };

    store_tokens_internal(app, &tokens)?;

    Ok(tokens)
}

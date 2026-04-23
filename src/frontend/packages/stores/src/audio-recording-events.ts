import { create } from 'zustand'

/**
 * Fan-out channel for real-time audio recording updates received from SignalR.
 * NotificationListener pushes into this store; collection/recordings pages read the
 * counter and refetch when it bumps.
 */
interface AudioRecordingEventsState {
  /** Monotonically increasing counter; pages can watch this to trigger refetches. */
  revision: number
  /** The most recently updated recording's ID, for surgical refetches. */
  lastRecordingId: string | null
  /** The most recently updated recording's collection, when known. */
  lastCollectionId: string | null
  /** Bump the revision with the latest update's IDs. */
  recordUpdate: (recordingId: string, collectionId: string | null) => void
}

export const useAudioRecordingEventsStore = create<AudioRecordingEventsState>()((set) => ({
  revision: 0,
  lastRecordingId: null,
  lastCollectionId: null,
  recordUpdate: (recordingId, collectionId) =>
    set((state) => ({
      revision: state.revision + 1,
      lastRecordingId: recordingId,
      lastCollectionId: collectionId,
    })),
}))

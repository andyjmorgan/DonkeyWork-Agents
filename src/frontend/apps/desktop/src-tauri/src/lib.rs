mod auth;

use auth::{PkceState, SharedPkceState};
use std::sync::Arc;
use tokio::sync::Mutex;

pub fn run() {
    let pkce_state: SharedPkceState = Arc::new(Mutex::new(PkceState::new()));

    tauri::Builder::default()
        .plugin(tauri_plugin_store::Builder::default().build())
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_deep_link::init())
        .manage(pkce_state)
        .setup(|app| {
            #[cfg(debug_assertions)]
            {
                use tauri::Manager;
                let window = app.get_webview_window("main").unwrap();
                window.open_devtools();
            }

            // Start background token refresh
            auth::start_token_refresh_loop(app.handle().clone());

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            auth::start_auth,
            auth::exchange_code,
            auth::get_tokens,
            auth::clear_tokens,
            auth::refresh_tokens,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

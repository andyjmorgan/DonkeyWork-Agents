mod auth;

use auth::{PkceState, SharedPkceState};
use std::sync::Arc;
use tauri::menu::{AboutMetadataBuilder, MenuBuilder, MenuItemBuilder, SubmenuBuilder};
use tauri::{Emitter, Manager};
use tokio::sync::Mutex;

fn build_menu(app: &tauri::AppHandle) -> tauri::Result<tauri::menu::Menu<tauri::Wry>> {
    let about_metadata = AboutMetadataBuilder::new()
        .name(Some("DonkeyWork"))
        .version(Some(env!("CARGO_PKG_VERSION")))
        .copyright(Some("DonkeyWork"))
        .build();

    let menu = MenuBuilder::new(app)
        // App menu (DonkeyWork)
        .item(
            &SubmenuBuilder::new(app, "DonkeyWork")
                .about(Some(about_metadata))
                .separator()
                .item(
                    &MenuItemBuilder::with_id("preferences", "Preferences...")
                        .accelerator("CmdOrCtrl+,")
                        .build(app)?,
                )
                .separator()
                .hide()
                .hide_others()
                .show_all()
                .separator()
                .quit()
                .build()?,
        )
        // File menu
        .item(
            &SubmenuBuilder::new(app, "File")
                .item(
                    &MenuItemBuilder::with_id("new_conversation", "New Conversation")
                        .accelerator("CmdOrCtrl+Shift+N")
                        .build(app)?,
                )
                .separator()
                .close_window()
                .build()?,
        )
        // Edit menu
        .item(
            &SubmenuBuilder::new(app, "Edit")
                .undo()
                .redo()
                .separator()
                .cut()
                .copy()
                .paste()
                .select_all()
                .build()?,
        )
        // View menu
        .item(
            &SubmenuBuilder::new(app, "View")
                .item(
                    &MenuItemBuilder::with_id("toggle_theme", "Toggle Theme")
                        .accelerator("CmdOrCtrl+Shift+T")
                        .build(app)?,
                )
                .build()?,
        )
        // Window menu
        .item(
            &SubmenuBuilder::new(app, "Window")
                .minimize()
                .maximize()
                .separator()
                .close_window()
                .build()?,
        )
        .build()?;

    Ok(menu)
}

pub fn run() {
    let pkce_state: SharedPkceState = Arc::new(Mutex::new(PkceState::new()));

    tauri::Builder::default()
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.set_focus();
            }
        }))
        .plugin(tauri_plugin_store::Builder::default().build())
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_updater::Builder::new().build())
        .plugin(tauri_plugin_process::init())
        .manage(pkce_state)
        .setup(|app| {
            // Build and set the application menu
            let menu = build_menu(app.handle())?;
            app.set_menu(menu)?;

            #[cfg(debug_assertions)]
            {
                let window = app.get_webview_window("main").unwrap();
                window.open_devtools();
            }

            // Start background token refresh
            auth::start_token_refresh_loop(app.handle().clone());

            Ok(())
        })
        .on_menu_event(|app, event| match event.id().as_ref() {
            "new_conversation" => {
                let _ = app.emit("menu-event", "new_conversation");
            }
            "toggle_theme" => {
                let _ = app.emit("menu-event", "toggle_theme");
            }
            "preferences" => {
                let _ = app.emit("menu-event", "preferences");
            }
            _ => {}
        })
        .invoke_handler(tauri::generate_handler![
            auth::start_auth,
            auth::get_tokens,
            auth::clear_tokens,
            auth::refresh_tokens,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

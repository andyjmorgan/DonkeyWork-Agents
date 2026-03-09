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
                    &MenuItemBuilder::with_id("new_item", "New Item")
                        .accelerator("CmdOrCtrl+N")
                        .build(app)?,
                )
                .item(
                    &MenuItemBuilder::with_id("close_item", "Close Item")
                        .accelerator("CmdOrCtrl+W")
                        .build(app)?,
                )
                .separator()
                .item(
                    &MenuItemBuilder::with_id("new_conversation", "New Conversation")
                        .accelerator("CmdOrCtrl+Shift+N")
                        .build(app)?,
                )
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
        // Go menu
        .item(
            &SubmenuBuilder::new(app, "Go")
                .item(
                    &MenuItemBuilder::with_id("go_chat", "Navi")
                        .accelerator("CmdOrCtrl+1")
                        .build(app)?,
                )
                .item(
                    &MenuItemBuilder::with_id("go_notes", "Notes")
                        .accelerator("CmdOrCtrl+2")
                        .build(app)?,
                )
                .item(
                    &MenuItemBuilder::with_id("go_research", "Research")
                        .accelerator("CmdOrCtrl+3")
                        .build(app)?,
                )
                .item(
                    &MenuItemBuilder::with_id("go_tasks", "Tasks")
                        .accelerator("CmdOrCtrl+4")
                        .build(app)?,
                )
                .item(
                    &MenuItemBuilder::with_id("go_projects", "Projects")
                        .accelerator("CmdOrCtrl+5")
                        .build(app)?,
                )
                .separator()
                .item(
                    &MenuItemBuilder::with_id("go_search", "Search History")
                        .accelerator("CmdOrCtrl+Shift+F")
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
            "new_conversation" | "new_item" | "close_item" | "go_chat" | "go_notes"
            | "go_research" | "go_tasks" | "go_projects" | "go_search" => {
                let _ = app.emit("menu-event", event.id().as_ref());
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

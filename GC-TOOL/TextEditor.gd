extends Control

# UI Elements (referenced from the scene tree)
@onready var text_edit: TextEdit = $VBoxContainer/TextEdit
@onready var open_dialog: FileDialog = $OpenFileDialog
@onready var save_dialog: FileDialog = $SaveFileDialog
@onready var ui_blocker: ColorRect = $UIBlocker
@onready var recent_menu: PopupMenu = $VBoxContainer/MenuBar/File/RecentlyOpened

# State Tracking & Persistence
var current_file_path: String = ""
var recent_files: Array = []
var last_saved_dir: String = ""
const CONFIG_FILE = "user://editor_settings.cfg"

enum FileMenuId { NEW, OPEN, SAVE, SAVE_AS }

func _ready() -> void:
	%File.add_submenu_node_item("Recently Opened", recent_menu)
	_cfg()

# --- Syslog ---

func _cfg() -> void:
	var config := ConfigFile.new()
	if config.load(CONFIG_FILE) == OK:
		recent_files = config.get_value("History", "recent_files", [])
		last_saved_dir = config.get_value("History", "last_saved_dir", "")
		if last_saved_dir != "":
			open_dialog.current_dir = last_saved_dir
			save_dialog.current_dir = last_saved_dir
	_update_recent_menu()

func _save_settings() -> void:
	var config := ConfigFile.new()
	config.set_value("History", "recent_files", recent_files)
	config.set_value("History", "last_saved_dir", last_saved_dir)
	config.save(CONFIG_FILE)

# --- INPUT SHORTCUTS ---

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("save"):
		save()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("new"):
		newf()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("open"):
		_open_dialog_modal(open_dialog)
		get_viewport().set_input_as_handled()

# --- MODAL BLOCKING ---

func _open_dialog_modal(dialog: FileDialog) -> void:
	ui_blocker.visible = true 
	dialog.show()

func _unblock_ui() -> void:
	ui_blocker.visible = false 

# --- SIGNAL HANDLERS ---

func _on_file_menu_id_pressed(id: int) -> void:
	match id:
		FileMenuId.NEW:
			newf()
		FileMenuId.OPEN:
			_open_dialog_modal(open_dialog)
		FileMenuId.SAVE:
			save()
		FileMenuId.SAVE_AS:
			_open_dialog_modal(save_dialog)

# --- RECENT FILES LOGIC ---

func _add_to_recent(path: String) -> void:
	if recent_files.has(path):
		recent_files.erase(path)
		
	recent_files.push_front(path)
	
	if recent_files.size() > 5:
		recent_files.pop_back()
		
	_update_recent_menu()
	_save_settings()

func _update_recent_menu() -> void:
	recent_menu.clear()
	
	if recent_files.is_empty():
		recent_menu.add_item("No recent files")
		recent_menu.set_item_disabled(0, true)
	else:
		for i in range(recent_files.size()):
			recent_menu.add_item(recent_files[i], i)

func _on_recent_file_selected(index: int) -> void:
	var path: String = recent_files[index]
	_load_file_from_path(path)

# --- CORE FILE OPERATIONS ---

func newf() -> void:
	current_file_path = ""
	text_edit.text = ""
	print("Created a new file.")

func _load_file_from_path(path: String) -> void:
	var file := FileAccess.open(path, FileAccess.READ)
	if file:
		text_edit.text = file.get_as_text()
		current_file_path = path
		_add_to_recent(path)
		print("Successfully opened: ", path)
	else:
		print("Error: Could not open file.")

func _on_open_file_selected(path: String) -> void:
	_unblock_ui()
	_load_file_from_path(path)

func save() -> void:
	if current_file_path == "":
		_open_dialog_modal(save_dialog)
	else:
		var file := FileAccess.open(current_file_path, FileAccess.WRITE)
		if file:
			file.store_string(text_edit.text)
			_add_to_recent(current_file_path)
			
			last_saved_dir = current_file_path.get_base_dir()
			open_dialog.current_dir = last_saved_dir
			save_dialog.current_dir = last_saved_dir
			_save_settings()
			
			print("Saved changes to: ", current_file_path)
		else:
			print("Error: Could not save file.")

func _on_save_file_selected(path: String) -> void:
	_unblock_ui()
	current_file_path = path
	save()

func _on_autosave() -> void:
	if current_file_path != "":
		save()

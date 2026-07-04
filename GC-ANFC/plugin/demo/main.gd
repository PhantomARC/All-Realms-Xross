extends Control

@onready var L = $L
# TODO: Update to match your plugin's name
const _plugin_name = "ARXNFC"
var nfc


func _ready():
	if Engine.has_singleton(_plugin_name):
		nfc = Engine.get_singleton(_plugin_name)
		# Connect signals
		nfc.tag_discovered.connect(_on_tag_discovered)
		nfc.tag_lost.connect(_on_tag_lost)
		nfc.nfc_error.connect(_on_nfc_error)
		nfc.tag_read.connect(_on_tag_read)
		
		# Start listening for tags in the background
		nfc.onNFC()
	else:
		print("GodotNFC plugin not found.")


func _on_tag_discovered(uid: String, tag_type: String):
	print("Tag found with UID: ", uid)
	print("Tag Encryption Status: ", tag_type)

func _on_tag_read(data:String):
	print(data)



func _on_tag_lost():
	print("Tag connection lost (User pulled phone away).")


func _on_nfc_error(msg: String):
	print("NFC Error: ", msg)


func _exit_tree():
	if nfc:
		nfc.offNFC()


func _on_read_pressed() -> void:
	L.text = "READ 227"
	nfc.setOpMode("READ")


func _on_write_pressed() -> void:
	L.text = "WRITE 227"
	nfc.setOpMode("WRITE")


func _on_sign_pressed() -> void:
	L.text = "SIGN 227"
	nfc.setOpMode("SIGN")

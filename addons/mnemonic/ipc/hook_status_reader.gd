class_name HookStatusReader
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookStatusSnapshotGd = preload("res://addons/mnemonic/ipc/hook_status_snapshot.gd")
const HookStatusReadResultGd = preload("res://addons/mnemonic/ipc/hook_status_read_result.gd")

var _paths: MnemonicDataRootPaths
var _contract_mismatch_warned: bool = false


func _init(paths: MnemonicDataRootPaths) -> void:
	_paths = paths


func read() -> HookStatusReadResult:
	var path := _paths.get_status_file()
	if not FileAccess.file_exists(path):
		return HookStatusReadResultGd.missing_file()

	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return HookStatusReadResultGd.parse_error("could not open")

	var text := file.get_as_text()
	file.close()

	var parsed = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		return HookStatusReadResultGd.parse_error("invalid json")

	var version_raw = parsed.get("contract_version", null)
	if version_raw == null or typeof(version_raw) not in [TYPE_INT, TYPE_FLOAT]:
		return HookStatusReadResultGd.parse_error("invalid contract_version")

	var found_version := int(version_raw)
	if found_version != Mc.IPC_CONTRACT_VERSION:
		if not _contract_mismatch_warned:
			push_warning(
				"Mnemonic: status.json contract_version=%d (expected %d); status ignored."
				% [found_version, Mc.IPC_CONTRACT_VERSION]
			)
			_contract_mismatch_warned = true
		return HookStatusReadResultGd.contract_mismatch(found_version, Mc.IPC_CONTRACT_VERSION)

	var snapshot: HookStatusSnapshot = HookStatusSnapshotGd.from_dict(parsed)
	if snapshot == null:
		return HookStatusReadResultGd.parse_error("invalid snapshot fields")

	return HookStatusReadResultGd.ok(snapshot)

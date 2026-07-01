class_name HookHeuristicCatalog
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")

## Pipeline-only signals (still in catalog for Core parity; hidden from Capture settings UI).
const SETTINGS_UI_HIDDEN_TYPES: Array[String] = [
	"activity_packet",
	"playtest_stop",
	"debug_session_stop",
	"editor_focus_changed",
]


static func is_settings_ui_visible(type_id: String) -> bool:
	return type_id not in SETTINGS_UI_HIDDEN_TYPES


static func entries_for_settings_ui() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for entry in entries():
		if is_settings_ui_visible(str(entry.get("type", ""))):
			result.append(entry)
	return result


static func entries() -> Array[Dictionary]:
	return [
		_entry(
			"playtest_start",
			"Playtest start",
			"Editor playtest session started",
			"playtest",
			7,
			5,
		),
		_entry(
			"playtest_ongoing",
			"Playtest ongoing",
			"Middle segment of a long play (no stop yet); kept even if disabled in settings",
			"playtest",
			7,
			1,
		),
		_entry(
			"playtest_stop",
			"Playtest stop",
			"Playtest session ended (no score)",
			"playtest",
			0,
			0,
		),
		_entry(
			"rapid_playtest",
			"Rapid playtest",
			"Several playtests in a short window",
			"playtest",
			9,
			0,
		),
		_entry(
			"long_playtest",
			"Long playtest",
			"Playtest ran longer than the long threshold",
			"playtest",
			8,
			0,
		),
		_entry(
			"runtime_error",
			"Runtime error",
			"Script/runtime failure during playtest",
			"playtest",
			9,
			3,
		),
		_entry(
			"scene_save",
			"Scene save",
			"A scene was saved in the editor",
			"editor",
			5,
			3,
		),
		_entry(
			"scene_transition",
			"Scene transition",
			"Active scene changed in the editor",
			"editor",
			4,
			2,
		),
		_entry(
			"save_burst",
			"Save burst",
			"Multiple distinct scenes saved in a window",
			"editor",
			6,
			0,
		),
		_entry(
			"iteration_cycle",
			"Iteration cycle",
			"Playtest soon after a scene save",
			"editor",
			10,
			0,
		),
		_entry(
			"git_commit",
			"Git commit",
			"Git commit recorded during the segment",
			"git",
			9,
			1,
		),
		_entry(
			"git_branch_change",
			"Git branch change",
			"Git branch changed during the segment",
			"git",
			6,
			1,
		),
		_entry(
			"git_push",
			"Git push",
			"Local commits were pushed to the upstream remote",
			"git",
			8,
			1,
		),
		_entry(
			"debug_session_start",
			"Debug session start",
			"Debugger attached during a playtest",
			"playtest",
			6,
			1,
		),
		_entry(
			"debug_session_stop",
			"Debug session stop",
			"Debugger session ended (no score)",
			"playtest",
			0,
			0,
		),
		_entry(
			"script_save",
			"Script save",
			"A script or shader file was saved in the editor",
			"editor",
			5,
			4,
		),
		_entry(
			"editor_focused_session",
			"Editor focused session",
			"Sustained focus on one editor screen without playtest",
			"editor",
			8,
			1,
		),
		_entry(
			"commit_after_playtest",
			"Commit after playtest",
			"Commit shortly after a playtest ended",
			"git",
			10,
			0,
		),
		_entry(
			"resource_saved",
			"Resource saved",
			"A non-scene project file was saved (script, data, etc.)",
			"editor",
			4,
			4,
		),
		_entry(
			"editor_focus_changed",
			"Editor focus changed",
			"Main editor screen changed (feeds activity packets; not scored)",
			"editor",
			0,
			0,
		),
		_entry(
			"activity_packet",
			"Activity packet",
			"Segment-scaled rolling summary of editor activity (internal; not scored)",
			"editor",
			0,
			0,
		),
		_entry(
			"resource_burst",
			"Resource burst",
			"Several distinct non-scene files saved in a window",
			"editor",
			6,
			0,
		),
		_entry(
			"edit_intensity",
			"Edit intensity",
			"Sustained editor saves/transitions with little playtest",
			"editor",
			8,
			2,
		),
		_entry(
			"scene_hopping",
			"Scene hopping",
			"Many scene changes without playtest in the window",
			"editor",
			6,
			1,
		),
		_entry(
			"script_focus",
			"Script focus",
			"Script editor dominated the activity window with resource saves and little playtest",
			"editor",
			7,
			1,
		),
		_entry(
			"layout_focus",
			"Layout focus",
			"2D or 3D editor dominated the window with scene transitions and no playtest",
			"editor",
			6,
			1,
		),
		_entry(
			"checkpoint_after_work",
			"Checkpoint after work",
			"Git commit soon after an edit-intensity window",
			"git",
			9,
			0,
		),
		_entry(
			"long_edit_span",
			"Long edit span",
			"Long segment of editor work without playtest",
			"editor",
			7,
			1,
		),
	]


static func _entry(
	type_id: String,
	label: String,
	description: String,
	category: String,
	default_weight: int,
	default_cap: int,
) -> Dictionary:
	return {
		"type": type_id,
		"label": label,
		"description": description,
		"category": category,
		"default_weight": default_weight,
		"default_cap": default_cap,
	}


static func entries_for_category(category: String) -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for entry in entries():
		if str(entry.get("category", "")) == category:
			result.append(entry)
	return result

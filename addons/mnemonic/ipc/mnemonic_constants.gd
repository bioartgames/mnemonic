extends RefCounted

## Keep in sync with mnemonic-core/src/Mnemonic.Core/MnemonicConstants.cs

const DATA_ROOT_FOLDER_NAME := "Mnemonic"
const CORE_PROCESS_IMAGE_NAME := "Mnemonic.Windows.exe"
const IPC_CONTRACT_VERSION := 1
const GIT_POLL_INTERVAL_SEC := 10.0
## Sync with MnemonicConstants.GitPushDedupeWindowSeconds
const GIT_PUSH_DEDUPE_WINDOW_SECONDS := 120.0
## Sync with MnemonicConstants.EditorFocusedSessionMinSeconds
const EDITOR_FOCUSED_SESSION_MIN_SECONDS := 600.0
## Sync with MnemonicConstants.EditorFocusedSessionEmitCooldownSeconds
const EDITOR_FOCUSED_SESSION_EMIT_COOLDOWN_SECONDS := 600.0
## Sync with MnemonicConstants.FocusSessionTickSeconds
const FOCUS_SESSION_TICK_SEC := 30.0
const SCRIPT_SAVE_EXTENSIONS: Array[String] = [".gd", ".gdshader", ".gdc"]
const PLAYTEST_POLL_INTERVAL_SEC := 0.5
## Sync with MnemonicConstants.SceneEventDedupeSeconds
const SCENE_EVENT_DEDUPE_SECONDS := 2.0
## Sync with MnemonicConstants.ResourceEventDedupeSeconds
const RESOURCE_EVENT_DEDUPE_SECONDS := 2.0
## Sync with MnemonicConstants.EditorFocusMinEmitIntervalSeconds
const EDITOR_FOCUS_MIN_EMIT_INTERVAL_SECONDS := 5.0
## Sync with MnemonicConstants.RuntimeErrorRateLimitPerMinute / RuntimeErrorRateWindowSeconds
const RUNTIME_ERROR_RATE_LIMIT_PER_MINUTE := 10
const RUNTIME_ERROR_RATE_WINDOW_SECONDS := 60.0
## Sync with MnemonicConstants.ScoreCapRuntimeError
const SCORE_CAP_RUNTIME_ERROR := 3
const GIT_JSONL_DEDUPE_TAIL_BYTES := 65536
const STATUS_POLL_INTERVAL_SEC := 2.0
const CORE_PROCESS_PROBE_CACHE_MS := 1500
const CLIP_THUMB_BATCH_SIZE := 4
const CLIPS_LOADING_MESSAGE := "Loading clips…"
const SETTINGS_KEY_DRAW_MOUSE := "draw_mouse"
const SETTINGS_DEFAULT_DRAW_MOUSE := true
const SETTINGS_KEY_SEGMENT_DURATION_SECONDS := "segment_duration_seconds"
const SETTINGS_DEFAULT_SEGMENT_DURATION_SECONDS := 120
const SEGMENT_DURATION_MIN_SEC := 30
const SEGMENT_DURATION_MAX_SEC := 600
const SEGMENT_DURATION_STEP_SEC := 30
const SETTINGS_KEY_PRESERVE_THRESHOLD := "preserve_threshold"
const SETTINGS_DEFAULT_PRESERVE_THRESHOLD := 10
const PRESERVE_THRESHOLD_MIN := 1
const PRESERVE_THRESHOLD_MAX := 500
const SETTINGS_KEY_HIGHLIGHT_SCORE_MIN := "highlight_score_min"
const SETTINGS_DEFAULT_HIGHLIGHT_SCORE_MIN := 25
const SETTINGS_KEY_NOTABLE_SCORE_MIN := "notable_score_min"
const SETTINGS_DEFAULT_NOTABLE_SCORE_MIN := 10
const SIGNIFICANCE_TIER_LABEL_MANUAL := "Manual"
const SIGNIFICANCE_TIER_LABEL_NOTABLE := "Notable"
const SIGNIFICANCE_TIER_LABEL_HIGHLIGHT := "Highlight"
const SEGMENT_HISTORY_FILE_NAME := "segment_history.jsonl"
const SEGMENT_HISTORY_CONTRACT_VERSION := 1
const EDITOR_SCENE_FILE_NAME := "editor_scene.json"
const EDITOR_SCENE_CONTRACT_VERSION := 1
const SETTINGS_KEY_SEGMENT_HISTORY_MAX_ENTRIES := "segment_history_max_entries"
const SETTINGS_DEFAULT_SEGMENT_HISTORY_MAX_ENTRIES := 200
const SEGMENT_HISTORY_MAX_ENTRIES_MIN := 10
const SEGMENT_HISTORY_MAX_ENTRIES_MAX := 1000
const TOOLTIP_SEGMENT_HISTORY_MAX_ENTRIES := (
	"How many closed segments to keep in the segment log file."
)
const SETTINGS_KEY_HEURISTICS := "heuristics"
const SETTINGS_KEY_START_RECORDING_ON_LAUNCH := "start_recording_on_launch"
const SETTINGS_DEFAULT_START_RECORDING_ON_LAUNCH := true
const EDITOR_SETTING_AUTO_LAUNCH_CORE := "mnemonic_hook/auto_launch_core"
const EDITOR_SETTING_STOP_CORE_ON_EDITOR_EXIT := "mnemonic_hook/stop_core_on_editor_exit"
const EDITOR_SETTING_VERBOSE_LOGGING := "mnemonic_hook/verbose_logging"
const EDITOR_SETTING_DEFAULT_BOOL := false
const EXIT_CORE_COMMAND_FILE_NAME := "exit_core.json"
const CORE_SHUTDOWN_WAIT_SEC := 5.0
const CORE_SHUTDOWN_POLL_SEC := 0.2
const CORE_LAUNCH_WAIT_SEC := 15.0
const CORE_LAUNCH_POLL_SEC := 0.2
const CORE_RECORDING_WAIT_SEC := 10.0
const CAPTURE_STATE_RECORDING := "recording"
const CAPTURE_STATE_PAUSED := "paused"
const CAPTURE_STATE_IDLE := "idle"
const CAPTURE_STATE_ERROR := "error"
const UI_STOP_RECORDING := "Stop recording"
const UI_START_RECORDING := "Start recording"
const UI_STATUS_RECORDING_STOPPED := "Recording stopped"
const UI_SAVE_SEGMENT := "Save segment"
const UI_COPY_CLIP_METADATA := "Copy metadata"
const UI_FILTER_CLIPS := "Filter clips…"
const UI_REFRESH_CLIPS_TOOLTIP := "Refresh clips list"
const UI_SEARCH_CLIPS_PLACEHOLDER := "Search clips… (subject, tags, scenes)"
const UI_FILTER_TIER_ALL := "All clips"
const UI_CLEAR_FILTERS := "Clear"
const UI_SEGMENT_LOG := "Segment log…"
const UI_CLEAR_SEGMENT_LOG_TOOLTIP := "Clear segment log"
const UI_SEARCH_SEGMENTS_PLACEHOLDER := "Search segments…"
const UI_CAPTURE_PANEL := "Capture…"
const UI_PLAY_VIDEO := "Play video"
const UI_DELETE_CLIP := "Delete"
const UI_COPY_OUTLINE_TOOLTIP := "Copy outline"
const UI_FILTER_DATE_FROM := "From date…"
const UI_FILTER_DATE_TO := "To date…"
const UI_TOAST_OUTLINE_COPIED := "Outline copied"
const UI_TOAST_METADATA_COPIED := "Metadata copied"
const UI_TOAST_DELETE_FAILED := "Delete failed"
const UI_TOAST_CLIP_DELETED := "Clip deleted"
const UI_TOAST_STOP_FAILED := "Stop failed"
const UI_TOAST_START_FAILED := "Start failed"
const TOOLTIP_CLIP_ACTIONS := "Clip actions"
const TOOLTIP_SEGMENT_ACTIONS := "Segment actions"
const TOOLTIP_LIVE_SCORE_BELOW_THRESHOLD := "below auto-save threshold"
const TOOLTIP_LIVE_MANUAL_PRESERVE_QUEUED := (
	"Will be saved when this segment closes."
)
const LIVE_REC_TIER_QUEUED_SAVE := "queued_save"
const LIVE_SAVE_FADE_IN_SEC := 0.15
const LIVE_SAVE_FADE_OUT_SEC := 0.18
const LIVE_SAVE_PULSE_MIN := 0.12
const LIVE_SAVE_PULSE_MAX := 0.42
const LIVE_SAVE_PULSE_SPEED := 1.4
const LIVE_REC_DOT_PX := 12
const LIVE_REC_PULSE_MIN := 0.12
const LIVE_REC_PULSE_MAX := 0.42
const LIVE_REC_PULSE_SPEED := 2.8
const LIVE_REC_CLOSE_PHASE_PULSE_SPEED_MULT := 2.0
const LIVE_SAVE_PHASE_AWAIT_ACK := 0
const LIVE_SAVE_PHASE_AWAIT_CLOSE := 1
const LIVE_SAVE_FLAG_POLL_SEC := 0.2
## Must cover segment close after manual preserve (default segment length is 120s).
const LIVE_SAVE_FLAG_TIMEOUT_SEC := 150.0
const TOOLTIP_STOP_RECORDING := "Stop recording and quit Mnemonic."
const TOOLTIP_START_RECORDING := "Start recording. Launches Mnemonic if it is not running."
const EDITOR_SETTING_CORE_WINDOWS_EXE := "mnemonic_hook/core_windows_exe"
const EDITOR_SETTING_DOCK_CLIPS_SPLIT_OFFSET := "mnemonic_hook/dock_clips_split_offset"
const LAYOUT_SECTION_HOOK := "mnemonic_hook"
const LAYOUT_KEY_DOCK_SLOT := "dock_slot"
const DEFAULT_DOCK_SLOT := 2
const DOCK_SLOT_MIN := 0
const DOCK_SLOT_MAX_EXCLUSIVE := 9
const DOCK_SPLIT_UNSET := -1
const DOCK_SPLIT_TOP_MIN_PX := 0
const DOCK_SPLIT_BOTTOM_MIN_PX := 96
const DOCK_SPLIT_BOTTOM_CHROME_MIN_PX := 28
const DOCK_SPLIT_DEFAULT_RATIO := 0.22
const DOCK_SPLIT_COLLAPSED_TOLERANCE_PX := 3
const DOCK_SPLIT_CHROME_GAP_PX := 3
const TOOLTIP_SETTINGS_GEAR := "Recording and workflow settings."
const TOOLTIP_SETTINGS_CAPTURE_PANEL := (
	"Segment length, auto-save threshold, and scoring signals."
)
const TOOLTIP_WORKFLOW_AUTO_START_ON_OPEN := (
	"Start recording when you open this project in Godot. "
	+ "May add a short delay during project startup."
)
const TOOLTIP_WORKFLOW_STOP_ON_EXIT := (
	"Stop recording and quit Mnemonic when you close Godot."
)
const TOOLTIP_WORKFLOW_VERBOSE_LOGGING := (
	"Show Hook startup and lifecycle diagnostics in the Output panel."
)
const MENU_WORKFLOW_SUBMENU := "Recording"
const MENU_WORKFLOW_AUTO_START_ON_OPEN := "Start recording when Godot opens"
const MENU_WORKFLOW_STOP_ON_EXIT := "Stop recording when Godot closes"
const MENU_WORKFLOW_VERBOSE_LOGGING := "Verbose logging"
const UI_SEGMENT_LOG_NO_FILE := (
	"No segment history yet."
)
const TOOLTIP_RETENTION_SECTION := (
	"Segment length, auto-save threshold, and capture options. "
	+ "Some options need a new recording session."
)
const TOOLTIP_SEGMENT_DURATION := (
	"How long each segment runs before it closes."
)
const TOOLTIP_PRESERVE_THRESHOLD := (
	"Minimum score to auto-save at segment end. "
	+ "Save segment is only offered when the current score is below this threshold."
)
const TOOLTIP_NOTABLE_SCORE_MIN := (
	"Minimum score for Notable tier in the dock (tooltips, filter, badges). "
	+ "Does not change auto-save; only how clips are labeled."
)
const TOOLTIP_HIGHLIGHT_SCORE_MIN := (
	"Minimum score for Highlight tier in the dock (tooltips, filter, badges). "
	+ "Does not change auto-save; only how clips are labeled."
)
const TOOLTIP_DRAW_MOUSE := (
	"Show the mouse cursor in desktop capture."
)
const TOOLTIP_SIGNALS_SECTION := (
	"Turn signals on or off and set how much each adds to the score."
)
const TOOLTIP_SIGNAL_WEIGHT := "Points added to the segment score when this signal fires."
const TOOLTIP_SIGNAL_CAP := (
	"Max times this signal counts in one segment."
)
const TOOLTIP_SIGNAL_CAP_NA := "No per-segment limit for this signal."
const UI_DIALOG_CLEAR_SEGMENT_LOG_TITLE := "Clear segment log"
const UI_DIALOG_CLEAR_SEGMENT_LOG_TEXT := "Delete all segment history entries?"
const UI_DIALOG_DELETE_CLIP_TITLE := "Delete clip?"
const UI_TOAST_SETTING_SAVE_FAILED := "Setting save failed"
const UI_TOAST_SETTINGS_SAVED_RESTART := "Settings saved — restart capture to apply"
const UI_TOAST_SETTINGS_SAVED := "Settings saved"
const UI_MENU_REVEAL := "Reveal in file manager"
const UI_SETTINGS_SECTION_RETENTION := "Retention"
const UI_SETTINGS_SECTION_SIGNALS := "Signals"
const UI_SETTINGS_SEGMENT_LENGTH := "Segment length (s)"
const UI_SETTINGS_PRESERVE_THRESHOLD := "Preserve threshold"
const UI_SETTINGS_NOTABLE_SCORE := "Notable score"
const UI_SETTINGS_HIGHLIGHT_SCORE := "Highlight score"
const UI_SETTINGS_SEGMENT_LOG_RETENTION := "Segment log retention"
const UI_SETTINGS_CAPTURE_CURSOR := "Capture cursor"
const UI_SIGNAL_CATEGORY_PLAYTEST := "Playtest"
const UI_SIGNAL_CATEGORY_EDITOR := "Editor"
const UI_SIGNAL_CATEGORY_GIT := "Git"
const DOCK_REBUILD_WAIT_MS := 5000
const DOCK_REBUILD_POLL_SEC := 0.2
const THUMB_COMPACT_WIDTH_PX := 56
const THUMB_COMPACT_HEIGHT_PX := 32
const GRIP_HEIGHT_PX := 14
const GRIP_HANDLE_WIDTH_PX := 36
const GRIP_HANDLE_HEIGHT_PX := 2
const UI_SEGMENT_LOG_UNAVAILABLE := (
	"Segment history unavailable — start recording or check the Mnemonic data folder."
)
const UI_SEGMENT_LOG_NO_SEGMENTS := "No segments yet."
const UI_SEGMENT_LOG_NO_MATCH := "No segments match your search."
const UI_CLIPS_DATAROOT_UNAVAILABLE := "DataRoot unavailable."
const UI_CLIPS_NO_CLIPS := "No clips yet."
const UI_CLIPS_NO_MATCH := "No clips match filters."
const UI_CLIPS_INDEX_UNAVAILABLE := "Clip index unavailable — try Refresh again."

## Sync with MnemonicConstants.cs / TrayController.cs
const CLIPS_INDEX_FILE_NAME := "clips_index.json"
const CLIPS_INDEX_VERSION := 1
const REBUILD_CLIPS_INDEX_FILE_NAME := "rebuild_clips_index.json"
const SUGGESTED_GROUPS_FILE_NAME := "suggested_groups.json"
const SUGGESTED_GROUPS_VERSION := 1
const CLIP_FILTER_DATE_FORMAT_HINT := "YYYY-MM-DD"
const FILTER_APPLY_DEBOUNCE_SEC := 0.25
const CLIP_VIDEO_FILE_NAME := "video.mp4"
const CLIP_THUMB_FILE_NAME := "thumb.jpg"
const CLIP_THUMB_WIDTH_PX := 96
const CLIP_THUMB_HEIGHT_PX := 54
const CLIP_LIST_MAX := 50
const CLIP_DISPLAY_TITLE_MAX_LEN := 48
const TOOLTIP_GIT_BRANCH_MAX_LEN := 48
const TOOLTIP_GIT_SUBJECT_MAX_LEN := 72
const STATUS_ERROR_MAX_LEN := 120
const DOCK_MIN_WIDTH_PX := 200
const DOCK_COMPACT_WIDTH_PX := 260
const CLIPS_SCROLL_MIN_HEIGHT_PX := 0
const CLIPS_LIST_SHOW_MIN_PX := 16
const SETTINGS_PANEL_MIN_WIDTH_PX := 420
const SETTINGS_PANEL_MAX_HEIGHT_PX := 480
const SETTINGS_PANEL_SIGNALS_SCROLL_MAX_PX := 280

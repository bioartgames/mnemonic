class_name HookClipsRowBuilder
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookClipDisplayGd = preload("res://addons/mnemonic/clips/hook_clip_display.gd")
const HookClipThumbnailGd = preload("res://addons/mnemonic/clips/hook_clip_thumbnail.gd")
const HookClipSceneTagsGd = preload("res://addons/mnemonic/clips/hook_clip_scene_tags.gd")
const HookClipSearchGd = preload("res://addons/mnemonic/clips/hook_clip_search.gd")
const HookSignificanceTierGd = preload("res://addons/mnemonic/clips/hook_significance_tier.gd")
const HookLiveDisplayGd = preload("res://addons/mnemonic/clips/hook_live_display.gd")
const HookLiveScratchRevealGd = preload("res://addons/mnemonic/ui/hook_live_scratch_reveal.gd")


static func from_live_preview(
	snap: HookStatusSnapshot,
	paths: MnemonicDataRootPaths,
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
	live_close_phase_fn: Callable = Callable(),
	live_save_pending: bool = false,
	live_save_segment_index: int = -1,
) -> Dictionary:
	if snap == null or paths == null:
		return {}
	if not snap.recording or snap.state.strip_edges().to_lower() != Mc.CAPTURE_STATE_RECORDING:
		return {}
	if typeof(snap.live_clip_preview) != TYPE_DICTIONARY or snap.live_clip_preview.is_empty():
		return {}
	var live: Dictionary = snap.live_clip_preview
	var segment_index := int(live.get("segment_index", -1))
	var segment_id := str(live.get("segment_id", "")).strip_edges()
	if segment_index < 0 and segment_id.is_empty():
		return {}
	var capture_prefix := str(live.get("capture_prefix", "")).strip_edges()
	if capture_prefix.is_empty():
		capture_prefix = str(snap.capture_prefix).strip_edges()
	if segment_id.is_empty() and not capture_prefix.is_empty() and segment_index >= 0:
		segment_id = "%s_segment_%05d" % [capture_prefix, segment_index]
	elif segment_id.is_empty() and segment_index >= 0:
		segment_id = "segment_%05d" % segment_index
	var folder_abs := paths.get_clips_dir().path_join(segment_id)
	var reveal := HookLiveScratchRevealGd.resolve(paths.get_scratch_dir(), segment_index, capture_prefix)
	var reveal_abs := str(reveal.get("reveal_abs", ""))
	var scenes: Array = []
	var scenes_raw: Variant = live.get("scenes_active", [])
	if typeof(scenes_raw) == TYPE_ARRAY:
		for scene in scenes_raw:
			scenes.append(str(scene))
	var tags: Array = []
	var tags_raw: Variant = live.get("tags", [])
	if typeof(tags_raw) == TYPE_ARRAY:
		for tag in tags_raw:
			tags.append(str(tag))
	var signal_types: PackedStringArray = PackedStringArray()
	var signal_raw: Variant = live.get("signal_types", [])
	if typeof(signal_raw) == TYPE_ARRAY:
		for signal_type in signal_raw:
			signal_types.append(str(signal_type))
	var row := {
		"id": segment_id,
		"created_at": int(live.get("t_close_unix", 0)),
		"duration_seconds": int(live.get("duration_seconds", 0)),
		"score": int(live.get("score_preview", 0)),
		"commit_subject": str(live.get("commit_subject", "")),
		"git_branch": str(live.get("git_branch", "")),
		"git_commit": str(live.get("git_commit", "")),
		"scenes_active": scenes,
		"tags": tags,
		"ai_topics": [],
		"folder_abs": folder_abs,
		"reveal_abs": reveal_abs,
		"show_reveal": bool(reveal.get("show_reveal", false)),
		"reveal_menu_label": str(reveal.get("menu_label", "")),
		"video_abs": folder_abs.path_join(Mc.CLIP_VIDEO_FILE_NAME),
		"thumb_abs": "",
		"is_live_row": true,
		"live_segment_index": segment_index,
	}
	var t_close_unix := float(live.get("t_close_unix", 0.0))
	var t_open_unix := float(live.get("t_open_unix", 0.0))
	var duration_seconds := int(live.get("duration_seconds", 0))
	row["live_t_close_unix"] = t_close_unix
	row["live_t_open_unix"] = t_open_unix
	row["live_duration_seconds"] = duration_seconds
	var close_phase := ""
	var now_unix := float(Time.get_unix_time_from_system())
	if t_close_unix > 0.0 and int(t_close_unix) - int(now_unix) <= 0:
		if live_close_phase_fn.is_valid():
			close_phase = str(live_close_phase_fn.call(snap, segment_index))
	row["display_title"] = HookLiveDisplayGd.format_countdown_timer(
		t_close_unix,
		t_open_unix,
		duration_seconds,
		now_unix,
		close_phase,
	)
	row["live_score_line"] = HookLiveDisplayGd.format_live_score(row, preserve_threshold)
	var signal_summary := ", ".join(signal_types)
	var signal_opts := {
		"preserve_threshold": preserve_threshold,
		"highlight_score_min": highlight_score_min,
		"notable_score_min": notable_score_min,
		"is_live": true,
	}
	if not signal_summary.is_empty():
		signal_opts["signal_summary"] = signal_summary
	var manual_queued := HookSignificanceTierGd.manual_preserve_queued(
		segment_index,
		snap.pending_manual_preserve_segment_index,
		live_save_pending,
		live_save_segment_index,
	)
	if manual_queued:
		signal_opts["manual_preserve_queued"] = true
	row["manual_preserve_queued"] = manual_queued
	row["live_rec_tier_id"] = HookSignificanceTierGd.resolve_live_rec_tier(
		int(row.get("score", 0)),
		preserve_threshold,
		highlight_score_min,
		notable_score_min,
		segment_index,
		snap.pending_manual_preserve_segment_index,
		live_save_pending,
		live_save_segment_index,
	)
	row["tooltip"] = HookClipDisplayGd.format_devlog_tooltip(row, signal_opts)
	return row


static func from_index_entry(
	paths: MnemonicDataRootPaths,
	entry: Dictionary,
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> Dictionary:
	var id := str(entry.get("id", "")).strip_edges()
	if id.is_empty():
		return {}
	var folder_abs := paths.get_clips_dir().path_join(id)
	if not FileAccess.file_exists(folder_abs.path_join("clip.json")):
		return {}
	var scenes_active: Array = []
	var scenes_raw: Variant = entry.get("scenes_active", [])
	if typeof(scenes_raw) == TYPE_ARRAY:
		for s in scenes_raw:
			scenes_active.append(str(s))
	var tags: Array = []
	var tags_raw: Variant = entry.get("tags", [])
	if typeof(tags_raw) == TYPE_ARRAY:
		for t in tags_raw:
			tags.append(str(t))
	var ai_topics: Array = []
	var topics_raw: Variant = entry.get("ai_topics", [])
	if typeof(topics_raw) == TYPE_ARRAY:
		for topic in topics_raw:
			ai_topics.append(str(topic))

	var dic := {
		"id": id,
		"created_at": int(entry.get("created_at", 0)),
		"duration_seconds": int(entry.get("duration_seconds", 0)),
		"score": int(entry.get("score", 0)),
		"commit_subject": str(entry.get("commit_subject", "")),
		"git_branch": str(entry.get("git_branch", "")),
		"git_commit": str(entry.get("git_commit", "")),
		"scenes_active": scenes_active,
		"tags": tags,
		"ai_topics": ai_topics,
	}
	return from_clip_dict(
		paths, dic, folder_abs, preserve_threshold, highlight_score_min, notable_score_min
	)


static func from_clip_dict(
	paths: MnemonicDataRootPaths,
	dic: Dictionary,
	folder_abs: String = "",
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> Dictionary:
	var id := str(dic.get("id", "")).strip_edges()
	if folder_abs.is_empty() and paths != null and paths.is_valid():
		folder_abs = paths.get_clips_dir().path_join(id)

	var scenes_active: Array = []
	var scenes_raw: Variant = dic.get("scenes_active", [])
	if typeof(scenes_raw) == TYPE_ARRAY:
		for s in scenes_raw:
			scenes_active.append(str(s))

	var tags: Array = []
	var tags_raw: Variant = dic.get("tags", [])
	if typeof(tags_raw) == TYPE_ARRAY:
		for t in tags_raw:
			tags.append(str(t))
	tags = HookClipSceneTagsGd.merge_scene_tags(tags, scenes_active)

	var ai_topics: Array = []
	var topics_raw: Variant = dic.get("ai_topics", [])
	if typeof(topics_raw) == TYPE_ARRAY:
		for topic in topics_raw:
			ai_topics.append(str(topic))

	var row := {
		"id": id,
		"created_at": int(dic.get("created_at", 0)),
		"duration_seconds": int(dic.get("duration_seconds", 0)),
		"score": int(dic.get("score", 0)),
		"commit_subject": str(dic.get("commit_subject", "")),
		"git_branch": str(dic.get("git_branch", "")),
		"git_commit": str(dic.get("git_commit", "")),
		"scenes_active": scenes_active,
		"tags": tags,
		"ai_topics": ai_topics,
	}

	var thumb_abs := ""
	if not folder_abs.is_empty():
		thumb_abs = HookClipThumbnailGd.thumb_abs_for_folder(folder_abs)

	var video_abs := folder_abs.path_join(Mc.CLIP_VIDEO_FILE_NAME) if not folder_abs.is_empty() else ""

	row["folder_abs"] = folder_abs
	row["video_abs"] = video_abs
	row["thumb_abs"] = thumb_abs
	var tier_id := HookSignificanceTierGd.classify_tier(
		int(row.get("score", 0)),
		preserve_threshold,
		highlight_score_min,
		notable_score_min,
	)
	row["significance_tier_id"] = tier_id
	row["display_title"] = HookClipDisplayGd.format_list_primary(row)
	row["tooltip"] = HookClipDisplayGd.format_devlog_tooltip(
		row,
		{
			"preserve_threshold": preserve_threshold,
			"highlight_score_min": highlight_score_min,
			"notable_score_min": notable_score_min,
		},
	)
	row["search_blob"] = HookClipSearchGd.build_search_blob(row)
	return row

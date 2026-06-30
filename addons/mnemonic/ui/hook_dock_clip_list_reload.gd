class_name HookDockClipListReload
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookClipsSourceGd = preload("res://addons/mnemonic_hook/clips/hook_clips_source.gd")


static func execute(ctx: HookDockClipListContext, mode: int) -> void:
	if ctx.stop_live_countdown.is_valid():
		ctx.stop_live_countdown.call()
	if ctx.clips_reload_signature.is_valid():
		ctx.last_clips_reload_signature = str(ctx.clips_reload_signature.call())
	ctx.clips_reload_pending = false
	ctx.clips_thumb_generation += 1
	if ctx.thumb_queue != null and ctx.thumb_queue.has_method("reset"):
		ctx.thumb_queue.reset(ctx.clips_thumb_generation)
	if ctx.sync_clips_thumb_generation.is_valid():
		ctx.sync_clips_thumb_generation.call(ctx.clips_thumb_generation)
	if ctx.clips_box != null:
		for child in ctx.clips_box.get_children():
			child.queue_free()

	if ctx.plugin == null:
		if ctx.add_clips_message.is_valid():
			ctx.add_clips_message.call(Mc.UI_CLIPS_DATAROOT_UNAVAILABLE)
		return

	var paths: MnemonicDataRootPaths = ctx.plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		if ctx.add_clips_message.is_valid():
			ctx.add_clips_message.call(Mc.UI_CLIPS_DATAROOT_UNAVAILABLE)
		return

	var preserve_threshold := int(ctx.preserve_threshold_for_tooltips.call(paths))
	var notable_score_min := int(
		ctx.notable_score_min_for_tooltips.call(paths, preserve_threshold)
	)
	var highlight_score_min := int(ctx.highlight_score_min_for_tooltips.call(paths))
	ctx.clip_row_preserve_threshold = preserve_threshold
	ctx.clip_row_notable_score_min = notable_score_min
	ctx.clip_row_highlight_score_min = highlight_score_min
	var has_live_row := false
	var live_info: Dictionary = {}
	if ctx.build_live_clip_row.is_valid():
		live_info = ctx.build_live_clip_row.call(
			paths, preserve_threshold, highlight_score_min, notable_score_min
		)
	if not live_info.is_empty() and ctx.add_clip_row.is_valid():
		ctx.add_clip_row.call(live_info)
		has_live_row = true

	var result: Dictionary = HookClipsSourceGd.list_grouped_for_dock(
		paths,
		ctx.filter_criteria,
		mode,
		preserve_threshold,
		highlight_score_min,
		notable_score_min,
	)
	var empty_reason := str(result.get("empty_reason", ""))
	if empty_reason == "no_clips":
		if not has_live_row and ctx.add_clips_empty_state.is_valid():
			ctx.add_clips_empty_state.call("no_clips")
		if ctx.schedule_thumb_queue_pump.is_valid():
			ctx.schedule_thumb_queue_pump.call()
		return
	if empty_reason == "no_match":
		if not has_live_row and ctx.add_clips_empty_state.is_valid():
			ctx.add_clips_empty_state.call("no_match")
		if ctx.schedule_thumb_queue_pump.is_valid():
			ctx.schedule_thumb_queue_pump.call()
		return
	if empty_reason == "index_unavailable":
		if not has_live_row and ctx.add_clips_empty_state.is_valid():
			ctx.add_clips_empty_state.call("index_unavailable")
		if ctx.schedule_thumb_queue_pump.is_valid():
			ctx.schedule_thumb_queue_pump.call()
		return

	var compact_groups: bool = not ctx.filter_criteria.is_empty()
	if bool(result.get("use_groups", false)):
		for group in result.get("groups", []):
			if typeof(group) != TYPE_DICTIONARY:
				continue
			if ctx.add_group_header.is_valid():
				ctx.add_group_header.call(group, compact_groups)
			for info in group.get("rows", []):
				if ctx.add_clip_row.is_valid():
					ctx.add_clip_row.call(info)
	else:
		for info in result.get("rows", []):
			if ctx.add_clip_row.is_valid():
				ctx.add_clip_row.call(info)

	if (
		is_instance_valid(ctx.segment_log_panel_outer)
		and ctx.segment_log_panel_outer.visible
		and ctx.reload_segment_log.is_valid()
	):
		ctx.reload_segment_log.call()
	if ctx.schedule_thumb_queue_pump.is_valid():
		ctx.schedule_thumb_queue_pump.call()

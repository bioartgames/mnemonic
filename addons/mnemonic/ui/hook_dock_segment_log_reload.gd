class_name HookDockSegmentLogReload
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const SegmentHistoryIoGd = preload("res://addons/mnemonic/ipc/segment_history_io.gd")
const HookClipSearchGd = preload("res://addons/mnemonic/clips/hook_clip_search.gd")


static func execute(ctx: HookDockSegmentLogContext) -> void:
	if not is_instance_valid(ctx.segment_log_list):
		return
	if ctx.clear_segment_log_list.is_valid():
		ctx.clear_segment_log_list.call()
	if ctx.plugin == null:
		if ctx.add_segment_log_empty_state.is_valid():
			ctx.add_segment_log_empty_state.call("data_root_unavailable")
		if ctx.update_segment_log_clear_enabled.is_valid():
			ctx.update_segment_log_clear_enabled.call()
		if ctx.sync_segment_log_layout.is_valid():
			ctx.sync_segment_log_layout.call()
		return
	var paths: MnemonicDataRootPaths = ctx.plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		if ctx.add_segment_log_empty_state.is_valid():
			ctx.add_segment_log_empty_state.call("data_root_unavailable")
		if ctx.update_segment_log_clear_enabled.is_valid():
			ctx.update_segment_log_clear_enabled.call()
		if ctx.sync_segment_log_layout.is_valid():
			ctx.sync_segment_log_layout.call()
		return
	var records: Array = SegmentHistoryIoGd.read_records(paths, Mc.CLIP_LIST_MAX)
	if records.is_empty():
		if not SegmentHistoryIoGd.history_file_exists(paths):
			if ctx.add_segment_log_empty_state.is_valid():
				ctx.add_segment_log_empty_state.call("no_file")
		elif ctx.add_segment_log_empty_state.is_valid():
			ctx.add_segment_log_empty_state.call("empty")
		if ctx.update_segment_log_clear_enabled.is_valid():
			ctx.update_segment_log_clear_enabled.call()
		if ctx.sync_segment_log_layout.is_valid():
			ctx.sync_segment_log_layout.call()
		return
	var query := ""
	if is_instance_valid(ctx.segment_log_search):
		query = ctx.segment_log_search.text.strip_edges()
	var filtered: Array = []
	for record in records:
		if typeof(record) != TYPE_DICTIONARY:
			continue
		if query.is_empty() or HookClipSearchGd.query_matches(
			SegmentHistoryIoGd.build_search_blob(record),
			query,
		):
			filtered.append(record)
	if filtered.is_empty():
		if query.is_empty():
			if ctx.add_segment_log_empty_state.is_valid():
				ctx.add_segment_log_empty_state.call("empty")
		elif ctx.add_segment_log_empty_state.is_valid():
			ctx.add_segment_log_empty_state.call("no_match")
		if ctx.update_segment_log_clear_enabled.is_valid():
			ctx.update_segment_log_clear_enabled.call()
		if ctx.sync_segment_log_layout.is_valid():
			ctx.sync_segment_log_layout.call()
		return
	for record in filtered:
		if typeof(record) != TYPE_DICTIONARY:
			continue
		var rtl := RichTextLabel.new()
		rtl.text = SegmentHistoryIoGd.format_tooltip(record)
		rtl.tooltip_text = ""
		if ctx.theme != null:
			ctx.theme.apply_segment_log_row_rich_text(rtl)
			var preserved := bool(record.get("preserved", false))
			if not preserved:
				rtl.add_theme_color_override(
					"default_color",
					ctx.theme.color_editor("warning_color"),
				)
		ctx.segment_log_list.add_child(rtl)
	if ctx.update_segment_log_clear_enabled.is_valid():
		ctx.update_segment_log_clear_enabled.call()
	if ctx.sync_segment_log_layout.is_valid():
		ctx.sync_segment_log_layout.call()

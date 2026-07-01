class_name HookDevlogOutline
extends RefCounted

const HookTimeDisplayGd = preload("res://addons/mnemonic/clips/hook_time_display.gd")


static func format_group(group: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray()
	var label := str(group.get("label", "")).strip_edges()
	if label.is_empty():
		label = "Clip group"
	lines.append("# %s" % label)

	for row in group.get("rows", []):
		if typeof(row) != TYPE_DICTIONARY:
			continue
		var created_at := int(row.get("created_at", 0))
		var when := ""
		if created_at > 0:
			when = HookTimeDisplayGd.format_local_datetime(created_at)
		var subject := str(row.get("commit_subject", "")).strip_edges()
		if subject.is_empty():
			subject = str(row.get("display_title", row.get("id", "")))
		var clip_id := str(row.get("id", ""))
		if when.is_empty():
			lines.append("- %s (%s)" % [subject, clip_id])
		else:
			lines.append("- %s — %s (%s)" % [when, subject, clip_id])

	return "\n".join(lines)

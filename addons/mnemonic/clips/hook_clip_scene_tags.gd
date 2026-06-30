class_name HookClipSceneTags
extends RefCounted

# Keep slug/stop rules in sync with mnemonic-core ClipSceneTagDeriver.cs

const _STOP_SEGMENTS: Array[String] = ["res", "scenes", "levels", "scripts", "ui"]
const _MAX_TAGS_PER_CLIP := 12
const _MIN_SEGMENT_LENGTH := 2


static func merge_scene_tags(tags: Array, scenes_active: Array) -> Array:
	var seen: Dictionary = {}
	var out: Array = []
	for t in tags:
		var key := str(t).to_lower()
		if key.is_empty() or seen.has(key):
			continue
		seen[key] = true
		out.append(str(t))

	for derived in derive_tags(scenes_active):
		var dkey := derived.to_lower()
		if seen.has(dkey):
			continue
		seen[dkey] = true
		out.append(derived)

	out.sort()
	return out


static func derive_tags(scenes_active: Array) -> PackedStringArray:
	var seen: Dictionary = {}
	var out: PackedStringArray = PackedStringArray()
	for path in scenes_active:
		if out.size() >= _MAX_TAGS_PER_CLIP:
			break
		_add_segments_from_path(str(path), seen, out)
	return out


static func slug_segment(segment: String) -> String:
	var lower := segment.strip_edges().to_lower()
	var parts: PackedStringArray = PackedStringArray()
	var current := ""
	for i in range(lower.length()):
		var c := lower[i]
		if (c >= "a" and c <= "z") or (c >= "0" and c <= "9"):
			current += c
		elif not current.is_empty() and not current.ends_with("-"):
			current += "-"
	if current.ends_with("-"):
		current = current.substr(0, current.length() - 1)
	return current


static func _add_segments_from_path(path: String, seen: Dictionary, out: PackedStringArray) -> void:
	var trimmed := path.strip_edges()
	if trimmed.is_empty():
		return
	var without_scheme := trimmed
	if without_scheme.begins_with("res://"):
		without_scheme = without_scheme.substr(6)
	var parts := without_scheme.split("/", false)
	if parts.is_empty():
		return
	var file_name: String = parts[parts.size() - 1]
	var base_name := file_name
	var dot := base_name.rfind(".")
	if dot > 0:
		base_name = base_name.substr(0, dot)
	_try_add_slug(base_name, seen, out)
	for i in range(parts.size() - 1):
		_try_add_slug(parts[i], seen, out)


static func _try_add_slug(segment: String, seen: Dictionary, out: PackedStringArray) -> void:
	if out.size() >= _MAX_TAGS_PER_CLIP:
		return
	var slug := slug_segment(segment)
	if slug.length() < _MIN_SEGMENT_LENGTH:
		return
	if slug in _STOP_SEGMENTS:
		return
	var key := slug.to_lower()
	if seen.has(key):
		return
	seen[key] = true
	out.append(slug)

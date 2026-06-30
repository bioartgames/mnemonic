class_name HookClipSearch
extends RefCounted


static func build_search_blob(row: Dictionary) -> String:
	var parts: PackedStringArray = PackedStringArray()
	var id := str(row.get("id", "")).strip_edges()
	if not id.is_empty():
		parts.append(id)
	var subject := str(row.get("commit_subject", "")).strip_edges()
	if not subject.is_empty():
		parts.append(subject)
	var tags: Variant = row.get("tags", [])
	if typeof(tags) == TYPE_ARRAY:
		for t in tags:
			var tag := str(t).strip_edges()
			if not tag.is_empty():
				parts.append(tag)
	var scenes: Variant = row.get("scenes_active", [])
	if typeof(scenes) == TYPE_ARRAY:
		for s in scenes:
			var scene := str(s).strip_edges()
			if not scene.is_empty():
				parts.append(scene)
	var topics: Variant = row.get("ai_topics", [])
	if typeof(topics) == TYPE_ARRAY:
		for topic in topics:
			var t := str(topic).strip_edges()
			if not t.is_empty():
				parts.append(t)
	return " ".join(parts).to_lower()


static func query_matches(blob: String, query: String) -> bool:
	var normalized := _normalize_query(query)
	if normalized.is_empty():
		return true
	var tokens := normalized.split(" ", false)
	for token in tokens:
		if blob.find(token) < 0:
			return false
	return true


static func _normalize_query(query: String) -> String:
	var collapsed := query.strip_edges().to_lower()
	while collapsed.find("  ") >= 0:
		collapsed = collapsed.replace("  ", " ")
	return collapsed

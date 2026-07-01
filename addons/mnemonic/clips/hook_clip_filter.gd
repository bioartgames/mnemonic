class_name HookClipFilter
extends RefCounted

const HookClipSearchGd = preload("res://addons/mnemonic/clips/hook_clip_search.gd")


static func matches(row: Dictionary, criteria) -> bool:
	if criteria == null:
		return true

	var query := str(criteria.search_query).strip_edges()
	if not query.is_empty():
		var blob := str(row.get("search_blob", ""))
		if not HookClipSearchGd.query_matches(blob, query):
			return false

	if criteria.from_unix >= 0:
		if int(row.get("created_at", 0)) < criteria.from_unix:
			return false

	if criteria.to_unix >= 0:
		if int(row.get("created_at", 0)) > criteria.to_unix:
			return false

	var tier_filter := str(criteria.significance_tier_filter).strip_edges()
	if not tier_filter.is_empty():
		if str(row.get("significance_tier_id", "")) != tier_filter:
			return false

	return true

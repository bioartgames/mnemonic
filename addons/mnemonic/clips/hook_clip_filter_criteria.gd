class_name HookClipFilterCriteria
extends RefCounted

var search_query: String = ""
var from_unix: int = -1
var to_unix: int = -1
## Empty = all tiers; else highlight | notable | manual.
var significance_tier_filter: String = ""


func is_empty() -> bool:
	return (
		search_query.strip_edges().is_empty()
		and from_unix < 0
		and to_unix < 0
		and significance_tier_filter.strip_edges().is_empty()
	)


static func parse_date_start_utc(date_str: String) -> int:
	var trimmed := date_str.strip_edges()
	if trimmed.is_empty():
		return -1
	return _parse_date_bound_unix(trimmed, false)


static func parse_date_end_utc(date_str: String) -> int:
	var trimmed := date_str.strip_edges()
	if trimmed.is_empty():
		return -1
	return _parse_date_bound_unix(trimmed, true)


static func _parse_date_bound_unix(date_str: String, end_of_day: bool) -> int:
	var parts := date_str.split("-")
	if parts.size() != 3:
		return -1
	if not parts[0].is_valid_int() or not parts[1].is_valid_int() or not parts[2].is_valid_int():
		return -1
	var year := int(parts[0])
	var month := int(parts[1])
	var day := int(parts[2])
	if month < 1 or month > 12 or day < 1 or day > 31:
		return -1
	var hour := 23 if end_of_day else 0
	var minute := 59 if end_of_day else 0
	var second := 59 if end_of_day else 0
	var dict := {
		"year": year,
		"month": month,
		"day": day,
		"hour": hour,
		"minute": minute,
		"second": second,
	}
	return int(Time.get_unix_time_from_datetime_dict(dict))

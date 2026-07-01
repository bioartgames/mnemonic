class_name HookGitAheadCount
extends RefCounted

const GitRunnerGd = preload("res://addons/mnemonic/git/git_runner.gd")


static func get_ahead_count() -> int:
	if not _has_upstream():
		return 0
	var result := GitRunnerGd.run(PackedStringArray(["rev-list", "--count", "@{u}..HEAD"]))
	if not result.get("ok", false):
		return 0
	var text := GitRunnerGd.trim_one_line(str(result.get("stdout", "")))
	if text.is_empty():
		return 0
	return maxi(0, int(text))


static func try_parse_upstream() -> Dictionary:
	if not _has_upstream():
		return {}
	var result := GitRunnerGd.run(PackedStringArray(["rev-parse", "--abbrev-ref", "@{u}"]))
	if not result.get("ok", false):
		return {}
	var upstream := GitRunnerGd.trim_one_line(str(result.get("stdout", "")))
	if upstream.is_empty():
		return {}
	var slash := upstream.find("/")
	if slash <= 0 or slash >= upstream.length() - 1:
		return {}
	return {
		"remote": upstream.substr(0, slash),
		"branch": upstream.substr(slash + 1),
	}


static func _has_upstream() -> bool:
	var result := GitRunnerGd.run(PackedStringArray(["rev-parse", "--abbrev-ref", "@{u}"]))
	if not result.get("ok", false):
		return false
	return not GitRunnerGd.trim_one_line(str(result.get("stdout", ""))).is_empty()

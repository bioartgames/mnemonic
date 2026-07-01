class_name HookGitPoll
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const GitRunnerGd = preload("res://addons/mnemonic/git/git_runner.gd")
const GitAheadCountGd = preload("res://addons/mnemonic/git/git_ahead_count.gd")
const SessionEventJsonGd = preload("res://addons/mnemonic/events/session_event_json.gd")
const JsonlEventAppenderGd = preload("res://addons/mnemonic/events/jsonl_event_appender.gd")
const JsonlGitDeduperGd = preload("res://addons/mnemonic/events/jsonl_git_deduper.gd")

var _paths: MnemonicDataRootPaths
var _events_path: String = ""
var _last_head: String = ""
var _last_branch: String = ""
var _last_ahead: int = 0
var _probe_disabled: bool = false
var _git_warned_failure: bool = false
var _baseline_initialized := false


func _init(paths: MnemonicDataRootPaths) -> void:
	_paths = paths
	_events_path = paths.get_session_events_file()


func initialize_baseline() -> void:
	if _probe_disabled:
		return
	var snapshot := _read_snapshot()
	if snapshot.is_empty():
		return
	_last_head = str(snapshot.get("head", ""))
	_last_branch = str(snapshot.get("branch", ""))
	_last_ahead = GitAheadCountGd.get_ahead_count()


func baseline_initialized() -> bool:
	return _baseline_initialized


func tick() -> void:
	if _probe_disabled:
		return

	if not _baseline_initialized:
		initialize_baseline()
		_baseline_initialized = true
		return

	var rh := GitRunnerGd.run(PackedStringArray(["rev-parse", "HEAD"]))
	if not rh.get("ok", false):
		_disable_probe()
		return

	var head := GitRunnerGd.trim_one_line(str(rh.get("stdout", "")))
	if head.is_empty():
		_disable_probe()
		return

	var branch := ""
	var rb := GitRunnerGd.run(PackedStringArray(["rev-parse", "--abbrev-ref", "HEAD"]))
	if rb.get("ok", false):
		branch = GitRunnerGd.trim_one_line(str(rb.get("stdout", "")))

	var old_head := _last_head
	var old_branch := _last_branch

	if head != old_head:
		var subject := ""
		var rs := GitRunnerGd.run(PackedStringArray(["log", "-1", "--pretty=%s"]))
		if rs.get("ok", false):
			subject = GitRunnerGd.trim_one_line(str(rs.get("stdout", "")))
		if not JsonlGitDeduperGd.has_recent_git_commit(_events_path, head):
			var t := float(Time.get_unix_time_from_system())
			JsonlEventAppenderGd.append(
				_events_path,
				SessionEventJsonGd.create_git_commit(t, head, subject)
			)

	if branch != old_branch and not branch.is_empty():
		if not JsonlGitDeduperGd.has_recent_git_branch_change(_events_path, branch):
			var t_branch := float(Time.get_unix_time_from_system())
			JsonlEventAppenderGd.append(
				_events_path,
				SessionEventJsonGd.create_git_branch_change(t_branch, branch)
			)

	var ahead := GitAheadCountGd.get_ahead_count()
	if (
		_last_ahead > 0
		and ahead == 0
		and head == old_head
		and not branch.is_empty()
		and not JsonlGitDeduperGd.has_recent_git_push(
			_events_path, branch, Mc.GIT_PUSH_DEDUPE_WINDOW_SECONDS
		)
	):
		var remote := ""
		var upstream := GitAheadCountGd.try_parse_upstream()
		if not upstream.is_empty():
			remote = str(upstream.get("remote", ""))
		var t_push := float(Time.get_unix_time_from_system())
		JsonlEventAppenderGd.append(
			_events_path,
			SessionEventJsonGd.create_git_push(t_push, branch, remote)
		)

	_last_ahead = ahead
	_last_head = head
	_last_branch = branch


func _read_snapshot() -> Dictionary:
	var rh := GitRunnerGd.run(PackedStringArray(["rev-parse", "HEAD"]))
	if not rh.get("ok", false):
		return {}
	var head := GitRunnerGd.trim_one_line(str(rh.get("stdout", "")))
	if head.is_empty():
		return {}
	var branch := ""
	var rb := GitRunnerGd.run(PackedStringArray(["rev-parse", "--abbrev-ref", "HEAD"]))
	if rb.get("ok", false):
		branch = GitRunnerGd.trim_one_line(str(rb.get("stdout", "")))
	return {"head": head, "branch": branch}


func _disable_probe() -> void:
	if not _git_warned_failure:
		push_warning("Mnemonic: git probe disabled (not a git repo or git unavailable).")
		_git_warned_failure = true
	_probe_disabled = true

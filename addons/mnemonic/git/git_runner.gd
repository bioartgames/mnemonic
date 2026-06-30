class_name HookGitRunner
extends RefCounted

## Runs `git -C <project_path> …` via OS.execute (no cwd kwarg).


static func run(subcommand_argv: PackedStringArray) -> Dictionary:
	var project_path := ProjectSettings.globalize_path("res://")
	var argv := PackedStringArray(["git", "-C", project_path])
	argv.append_array(subcommand_argv)
	var out: Array = []
	var exit_code: int = OS.execute(argv[0], argv.slice(1), out, true, false)
	var text := "".join(out)
	text = text.strip_edges()
	return {"ok": exit_code == 0, "exit_code": exit_code, "stdout": text}


static func trim_one_line(stdout: String) -> String:
	var s := stdout.strip_edges()
	var nl := s.find("\n")
	if nl != -1:
		s = s.substr(0, nl).strip_edges()
	return s

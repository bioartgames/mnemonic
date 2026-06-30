class_name HookPathGuard
extends RefCounted


static func normalize_abs(path: String) -> String:
	var p := path.strip_edges().replace("\\", "/")
	if p.is_empty():
		return ""
	return p.simplify_path()


static func is_absolute_path_under_root(candidate_abs: String, root_abs: String) -> bool:
	var child := normalize_abs(candidate_abs)
	var root := normalize_abs(root_abs)
	if child.is_empty() or root.is_empty():
		return false
	if not root.ends_with("/"):
		root += "/"
	if child == root.trim_suffix("/"):
		return true
	return child.begins_with(root)

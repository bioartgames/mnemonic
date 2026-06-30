class_name HookStatusReadResult
extends RefCounted

enum Code { OK, MISSING_FILE, PARSE_ERROR, CONTRACT_MISMATCH }

var code: Code = Code.MISSING_FILE
var snapshot: HookStatusSnapshot = null
var message: String = ""


static func ok(snapshot: HookStatusSnapshot) -> HookStatusReadResult:
	var result := HookStatusReadResult.new()
	result.code = Code.OK
	result.snapshot = snapshot
	return result


static func missing_file() -> HookStatusReadResult:
	var result := HookStatusReadResult.new()
	result.code = Code.MISSING_FILE
	result.message = "status.json missing"
	return result


static func parse_error(detail: String) -> HookStatusReadResult:
	var result := HookStatusReadResult.new()
	result.code = Code.PARSE_ERROR
	result.message = detail
	return result


static func contract_mismatch(found: int, expected: int) -> HookStatusReadResult:
	var result := HookStatusReadResult.new()
	result.code = Code.CONTRACT_MISMATCH
	result.message = "contract_version %d != %d" % [found, expected]
	return result

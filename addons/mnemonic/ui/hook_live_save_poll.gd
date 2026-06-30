class_name HookLiveSavePoll
extends RefCounted

enum Outcome { PENDING, ACKNOWLEDGED, COMPLETE, FAIL_PATHS, FAIL_TIMEOUT }

const PHASE_AWAIT_ACK := 0
const PHASE_AWAIT_CLOSE := 1


static func poll_session(
	snap: HookStatusSnapshot,
	saved_segment_index: int,
	deadline_ms: int,
	phase: int,
) -> Outcome:
	if snap == null:
		return Outcome.FAIL_PATHS
	if Time.get_ticks_msec() >= deadline_ms:
		return Outcome.FAIL_TIMEOUT
	if saved_segment_index < 0:
		return Outcome.FAIL_PATHS
	var pending := snap.pending_manual_preserve_segment_index
	if phase == PHASE_AWAIT_ACK:
		if pending == saved_segment_index:
			return Outcome.ACKNOWLEDGED
		return Outcome.PENDING
	if pending < 0:
		return Outcome.COMPLETE
	return Outcome.PENDING

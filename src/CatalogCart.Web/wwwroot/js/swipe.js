// Swipe-to-remove for cart rows.
//
// Browsers have no built-in swipe control, so this is a small gesture handler using
// Pointer Events (works for touch, pen and mouse drag). One listener on the list
// container handles every row, so rows Blazor adds or removes later just work.


const START_DISTANCE_PX = 8;     // Movement before decide it's a horizontal swipe.
const REMOVE_THRESHOLD = 0.35;   // Fraction of the row width that counts as "remove".
const ANIMATION_MS = 180;

export function attachSwipeToRemove(container, dotNetRef) {
    let gesture = null;

    function onPointerDown(event) {
        if (event.pointerType === "mouse" && event.button !== 0) return;

        const content = event.target.closest(".swipe-row__content");
        if (!content || !container.contains(content)) return;

        // Let buttons and inputs inside the row work normally.
        if (event.target.closest("button, a, input, select, textarea")) return;

        gesture = {
            content,
            row: content.closest(".swipe-row"),
            pointerId: event.pointerId,
            startX: event.clientX,
            startY: event.clientY,
            dx: 0,
            dragging: false,
        };
    }

    function onPointerMove(event) {
        if (!gesture || event.pointerId !== gesture.pointerId) return;

        const dx = event.clientX - gesture.startX;
        const dy = event.clientY - gesture.startY;

        if (!gesture.dragging) {
            if (Math.abs(dx) < START_DISTANCE_PX && Math.abs(dy) < START_DISTANCE_PX) return;

            // Mostly vertical: the user is scrolling, not swiping.
            if (Math.abs(dy) > Math.abs(dx)) {
                gesture = null;
                return;
            }

            gesture.dragging = true;
            gesture.content.setPointerCapture(event.pointerId);
            gesture.content.style.transition = "none";
            gesture.row.classList.add("is-swiping");
        }

        // Left swipe only, like most mobile list UIs.
        gesture.dx = Math.min(0, dx);
        gesture.content.style.transform = `translateX(${gesture.dx}px)`;
        gesture.row.classList.toggle("will-remove", passedThreshold(gesture));
        event.preventDefault();
    }

    function onPointerUp(event) {
        if (!gesture || event.pointerId !== gesture.pointerId) return;

        const finished = gesture;
        gesture = null;
        if (!finished.dragging) return;

        finished.content.style.transition = `transform ${ANIMATION_MS}ms ease-out`;

        if (passedThreshold(finished)) {
            removeRow(finished);
        } else {
            // Not far enough: snap back.
            finished.content.style.transform = "";
            finished.row.classList.remove("is-swiping", "will-remove");
        }
    }

    function onPointerCancel(event) {
        if (!gesture || event.pointerId !== gesture.pointerId) return;
        gesture.content.style.transition = `transform ${ANIMATION_MS}ms ease-out`;
        gesture.content.style.transform = "";
        gesture.row.classList.remove("is-swiping", "will-remove");
        gesture = null;
    }

    function passedThreshold(g) {
        return -g.dx > g.content.offsetWidth * REMOVE_THRESHOLD;
    }

    function removeRow({ content, row }) {
        const lineId = Number(row.dataset.lineId);
        content.style.transform = "translateX(-100%)";
        row.classList.add("is-removing");

        // Tell .NET once the slide-out finishes. The timeout is a fallback in case
        // transitionend doesn't fire.
        let notified = false;
        const notify = () => {
            if (notified) return;
            notified = true;
            dotNetRef.invokeMethodAsync("RemoveLineAsync", lineId);
        };
        content.addEventListener("transitionend", notify, { once: true });
        setTimeout(notify, ANIMATION_MS + 60);
    }

    // A native image/text drag would cancel the pointer gesture, so block it inside rows.
    function onDragStart(event) {
        if (event.target.closest?.(".swipe-row__content")) event.preventDefault();
    }

    container.addEventListener("pointerdown", onPointerDown);
    container.addEventListener("pointermove", onPointerMove);
    container.addEventListener("pointerup", onPointerUp);
    container.addEventListener("pointercancel", onPointerCancel);
    container.addEventListener("dragstart", onDragStart);

    return {
        dispose() {
            container.removeEventListener("dragstart", onDragStart);
            container.removeEventListener("pointerdown", onPointerDown);
            container.removeEventListener("pointermove", onPointerMove);
            container.removeEventListener("pointerup", onPointerUp);
            container.removeEventListener("pointercancel", onPointerCancel);
        },
    };
}

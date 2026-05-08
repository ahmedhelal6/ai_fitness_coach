import cv2
import traceback

from src.pipeline.realtime_pipeline import RealtimePipeline

from src.ui.ui_draw import (
    draw_hud,
    draw_decision_buttons,
    draw_report
)

# =========================================================
# INIT
# =========================================================
pipeline = RealtimePipeline()

cap = cv2.VideoCapture(0)

WINDOW = "AI Fitness Coach"

cv2.namedWindow(WINDOW)

clicked_point = None

# =========================================================
# MOUSE CALLBACK
# =========================================================
def mouse_callback(event, x, y, flags, param):

    global clicked_point

    if event == cv2.EVENT_LBUTTONDOWN:

        clicked_point = (x, y)


cv2.setMouseCallback(
    WINDOW,
    mouse_callback
)

# =========================================================
# CHECK CAMERA
# =========================================================
if not cap.isOpened():

    print("❌ Camera not opened")

    exit()

# =========================================================
# LOOP
# =========================================================
while True:

    try:

        # =================================================
        # READ FRAME
        # =================================================
        ret, frame = cap.read()

        if not ret:

            print("❌ Failed to read frame")

            continue

        # =================================================
        # PIPELINE
        # =================================================
        frame, exercise, reps, *_ = (
            pipeline.process_frame(frame)
        )

        # =================================================
        # CURRENT STATE
        # =================================================
        state = pipeline.current_state

        # =================================================
        # HUD
        # =================================================
        frame = draw_hud(

            frame,

            exercise,

            reps,

            state
        )

        # =================================================
        # AUTO PAUSE AFTER 10 REPS
        # =================================================
        if (

            reps > 0

            and reps % 10 == 0

            and not pipeline.session.awaiting_decision

            and not pipeline.session.show_report
        ):

            pipeline.session.trigger_decision()

        # =================================================
        # DECISION BUTTONS
        # =================================================
        if pipeline.session.awaiting_decision:

            frame, yes_box, no_box, reset_box = (

                draw_decision_buttons(

                    frame,

                    clicked_point
                )
            )

            # =============================================
            # HANDLE MOUSE CLICK
            # =============================================
            if clicked_point is not None:

                x, y = clicked_point

                # =========================================
                # RESUME
                # =========================================
                if (

                    yes_box[0] <= x <= yes_box[2]

                    and

                    yes_box[1] <= y <= yes_box[3]
                ):

                    pipeline.session.continue_session(
                        pipeline
                    )

                    clicked_point = None

                # =========================================
                # FINISH
                # =========================================
                elif (

                    no_box[0] <= x <= no_box[2]

                    and

                    no_box[1] <= y <= no_box[3]
                ):

                    pipeline.session.finish_session()

                    clicked_point = None

                # =========================================
                # RESTART
                # =========================================
                elif (

                    reset_box[0] <= x <= reset_box[2]

                    and

                    reset_box[1] <= y <= reset_box[3]
                ):

                    pipeline.session.restart_session(
                        pipeline
                    )

                    pipeline.full_reset()

                    clicked_point = None

        # =================================================
        # REPORT
        # =================================================
        if pipeline.session.show_report:

            frame = draw_report(

                frame,

                reps,

                state
            )

        # =================================================
        # SHOW
        # =================================================
        cv2.imshow(
            WINDOW,
            frame
        )

        # =================================================
        # KEYS
        # =================================================
        key = cv2.waitKey(1) & 0xFF

        # ESC
        if key == 27:

            break

        # RESET
        if key == ord('r'):

            pipeline.full_reset()

        # FINISH SESSION
        if key == ord('f'):

            pipeline.session.finish_session()

    # =====================================================
    # ERROR HANDLER
    # =====================================================
    except Exception as e:

        print("\n❌ ERROR DETECTED\n")

        print(e)

        traceback.print_exc()

        cv2.waitKey(0)

        break

# =========================================================
# CLOSE
# =========================================================
cap.release()

cv2.destroyAllWindows()

pipeline.close()
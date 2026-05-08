
import cv2

def draw_report_screen(frame, report):
    h, w, _ = frame.shape

    overlay = frame.copy()

    # خلفية
    cv2.rectangle(overlay, (50, 50), (w - 50, h - 50), (0, 0, 0), -1)
    cv2.addWeighted(overlay, 0.8, frame, 0.2, 0, frame)

    # العنوان
    cv2.putText(frame,
                "WORKOUT SUMMARY",
                (w // 2 - 220, 120),
                cv2.FONT_HERSHEY_SIMPLEX,
                1.2,
                (0, 255, 255),
                3)

    y = 200

    lines = [
        f"Exercise: {report['exercise']}",
        f"Total Reps: {report['total_reps']}",
        f"Average Quality: {report['avg_quality']}%",
        f"Best Rep: {report['best_rep']}%",
        f"Worst Rep: {report['worst_rep']}%",
    ]

    for line in lines:
        cv2.putText(frame,
                    line,
                    (100, y),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.9,
                    (255, 255, 255),
                    2)
        y += 50
 
    return frame


import json
from datetime import datetime


class SessionManager:

    def __init__(self):

        # =====================================================
        # SESSION DATA
        # =====================================================
        self.rep_scores = []

        self.exercise = None

        self.last_report = None

        # =====================================================
        # UI STATES
        # =====================================================
        self.awaiting_decision = False

        self.show_report = False

    # =========================================================
    # EXERCISE
    # =========================================================
    def update_exercise(self, exercise):

        if self.exercise is None:

            self.exercise = exercise

    # =========================================================
    # ADD REP
    # =========================================================
    def add_rep(self, quality):

        self.rep_scores.append(quality)

    # =========================================================
    # GENERATE REPORT
    # =========================================================
    def generate_report(self):

        if len(self.rep_scores) == 0:

            return None

        avg_quality = int(

            (
                sum(self.rep_scores)
                /
                len(self.rep_scores)
            )
        )

        report = {

            "exercise":
                self.exercise,

            "total_reps":
                len(self.rep_scores),

            "avg_quality":
                avg_quality,

            "best_rep":
                int(max(self.rep_scores)),

            "worst_rep":
                int(min(self.rep_scores)),
        }

        self.last_report = report

        # =====================================================
        # SAVE REPORT
        # =====================================================
        try:

            with open(

                "workout_reports.txt",

                "a",

                encoding="utf-8"

            ) as f:

                date_str = datetime.now().strftime(

                    "%Y-%m-%d %H:%M:%S"
                )

                line = (

                    f"[{date_str}] "

                    f"Exercise: {report['exercise']} | "

                    f"Reps: {report['total_reps']} | "

                    f"Avg Quality: {report['avg_quality']}% | "

                    f"Best: {report['best_rep']}% | "

                    f"Worst: {report['worst_rep']}%\n"
                )

                f.write(line)

        except Exception as e:

            print(
                f"Failed to save report: {e}"
            )

        return report

    # =========================================================
    # SHOW BUTTONS
    # =========================================================
    def trigger_decision(self):

        self.awaiting_decision = True

    # =========================================================
    # CONTINUE SESSION
    # =========================================================
    def continue_session(self, pipeline):

        self.awaiting_decision = False

        self.show_report = False

    # =========================================================
    # FINISH SESSION
    # =========================================================
    def finish_session(self):

        self.awaiting_decision = False

        self.show_report = True

        self.generate_report()

    # =========================================================
    # RESTART SESSION
    # =========================================================
    def restart_session(self, pipeline):

        self.awaiting_decision = False

        self.show_report = False

        # =====================================================
        # RESET SESSION DATA
        # =====================================================
        self.rep_scores.clear()

        self.exercise = None

        self.last_report = None

        # =====================================================
        # RESET PIPELINE
        # =====================================================
        pipeline.reset_session()

    # =========================================================
    # RESET EVERYTHING
    # =========================================================
    def reset(self):

        self.rep_scores.clear()

        self.exercise = None

        self.last_report = None

        self.awaiting_decision = False

        self.show_report = False
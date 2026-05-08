class RepCounter:
    def __init__(self, config, min_frames_confirm=3):
        self.config = config
        self.reps = 0
        self.stage = config.get("start_stage", None)
        
        self.min_frames_confirm = min_frames_confirm
        self.candidate_stage = None
        self.candidate_count = 0
        
        # New attributes for form tracking
        self.last_primary_angle = None
        self.secondary_angles_history = {} # Tracks min/max during the rep

        # Performance tracking attributes
        self.total_score = 0
        self.combo = 0
        self.best_combo = 0

    def reset(self, new_config=None):
        if new_config is not None:
            self.config = new_config
        self.reps = 0
        self.stage = self.config.get("start_stage", None)
        self.last_primary_angle = None
        self.candidate_stage = None
        self.candidate_count = 0
        self.secondary_angles_history = {}
        self.total_score = 0
        self.combo = 0
        self.best_combo = 0

    def _set_stage_with_confirm(self, new_stage):
        if new_stage is None:
            self.candidate_stage = None
            self.candidate_count = 0
            return False, self.stage

        if self.candidate_stage != new_stage:
            self.candidate_stage = new_stage
            self.candidate_count = 1
            return False, self.stage

        self.candidate_count += 1

        if self.candidate_count >= self.min_frames_confirm and self.stage != new_stage:
            old_stage = self.stage
            self.stage = new_stage
            self.candidate_stage = None
            self.candidate_count = 0
            return True, old_stage

        return False, self.stage

    def _update_secondary_history(self, angles_dict):
        # angles_dict looks like: {"primary": 120, "back": 145}
        for key, value in angles_dict.items():
            if key == "primary" or value is None:
                continue
            if key not in self.secondary_angles_history:
                self.secondary_angles_history[key] = {"min": value, "max": value}
            else:
                self.secondary_angles_history[key]["min"] = min(self.secondary_angles_history[key]["min"], value)
                self.secondary_angles_history[key]["max"] = max(self.secondary_angles_history[key]["max"], value)

    def _evaluate_form(self):
        quality = 100
        feedbacks = []
        
        form_checks = self.config.get("form_checks", [])
        for check in form_checks:
            angle_key = check.get("angle_key")
            if angle_key and angle_key in self.secondary_angles_history:
                hist = self.secondary_angles_history[angle_key]
                # If the angle went below the allowed minimum, or above allowed maximum
                if hist["min"] < check.get("min_angle", 0) or hist["max"] > check.get("max_angle", 360):
                    quality -= check.get("penalty", 20)
                    feedbacks.append(check.get("message", "Bad form"))
        
        # Reset history for the next rep
        self.secondary_angles_history = {}
        
        return max(0, quality), feedbacks

    def _update_performance(self, quality_score):
        if quality_score >= 90:
            quality_class = "perfect"
        elif quality_score >= 75:
            quality_class = "good"
        else:
            quality_class = "bad"

        if quality_class == "perfect":
            self.combo += 1
        else:
            self.combo = 0

        self.best_combo = max(self.best_combo, self.combo)

        if quality_class == "perfect":
            base = 100
        elif quality_class == "good":
            base = 80
        else:
            base = 50

        bonus = self.combo * 5
        total = base + bonus
        self.total_score += total

        return total, quality_class

    def update(self, angles_dict):
        # If passed a float (backward compatibility), wrap it in dict
        if not isinstance(angles_dict, dict):
            angles_dict = {"primary": angles_dict}

        primary_angle = angles_dict.get("primary")
        self.last_primary_angle = primary_angle
        
        if primary_angle is not None:
            self._update_secondary_history(angles_dict)

        full_angle = self.config["full_angle"]
        contract_angle = self.config["contract_angle"]
        mode = self.config["mode"]

        rep_completed = False
        quality = 100
        feedbacks = []

        if primary_angle is not None:
            if mode == "down_up":
                if primary_angle >= full_angle:
                    self._set_stage_with_confirm("down")

                elif primary_angle <= contract_angle:
                    changed, old_stage = self._set_stage_with_confirm("up")
                    if changed and old_stage == "down":
                        self.reps += 1
                        rep_completed = True

            elif mode == "up_down_up":
                if primary_angle <= contract_angle:
                    self._set_stage_with_confirm("down")

                elif primary_angle >= full_angle:
                    changed, old_stage = self._set_stage_with_confirm("up")
                    if changed and old_stage == "down":
                        self.reps += 1
                        rep_completed = True

        if rep_completed:
            quality, feedbacks = self._evaluate_form()
            gained, quality_class = self._update_performance(quality)
        else:
            quality = None
            feedbacks = None
            gained = None
            quality_class = None

        return {
            "reps": self.reps,
            "stage": self.stage,
            "angle": self.last_primary_angle,
            "rep_completed": rep_completed,
            "quality": quality,
            "feedback": feedbacks,
            "gained_score": gained,
            "combo": self.combo,
            "best_combo": self.best_combo,
            "total_score": self.total_score,
            "quality_class": quality_class
        }
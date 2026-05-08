namespace Ai_Fitness_Coach.DTOs
{
    public class ProgressOverviewDto
    {
        // Top Card
        public double OverallCompletion { get; set; }

        // Stats Grid
        public double TotalVolume { get; set; }
        public double AvgSessionVolume { get; set; }
        public int SessionsThisWeek { get; set; }
        public double WeeklyVolume { get; set; }
        public double AvgWeeklyVolume { get; set; }

        // Highlights
        public string? TopVolumeExercise { get; set; }
        public string? TopWorkout { get; set; }
        public int SavedWorkouts { get; set; }
        public int ExercisesInsidePlans { get; set; }

        // Graph Data
        public List<VolumeTrendDto>? VolumeTrend { get; set; }

    }
}

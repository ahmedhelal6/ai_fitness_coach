using Ai_Fitness_Coach.Data;
using Ai_Fitness_Coach.DTOs;
using Ai_Fitness_Coach.Models;
using Microsoft.EntityFrameworkCore;

namespace Ai_Fitness_Coach.Services
{
    public class WorkoutService : IWorkoutService
    {
        private readonly ApplicationDbContext _context;

        public WorkoutService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, List<ExerciseDto>>> GetAllExercisesAsync()
        {
            var exercises = await _context.Exercises
                .Where(e => !e.IsDeleted)
                .ToListAsync();

            // Group by ExerciseType (which you mentioned is the Category)
            // We convert the key to lowercase to match the Flutter dev's 'chest', 'back' keys
            return exercises
                .GroupBy(e => e.ExerciseType.ToLower())
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(e => new ExerciseDto
                    {
                        ExerciseId = e.Id,
                        Name = e.Name,
                        TargetMuscles = string.IsNullOrWhiteSpace(e.TargetMuscles)
                            ? new List<string>()
                            : e.TargetMuscles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        Equipments = string.IsNullOrWhiteSpace(e.Equipment)
                            ? new List<string>()
                            : e.Equipment.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        ImageUrl = e.ImageUrl ?? "",
                        VideoUrl = e.VideoUrl ?? "",
                        ExerciseType = e.ExerciseType
                    }).ToList()
                );
        }

        public async Task<WorkoutPlanDto> CreateWorkoutPlanAsync(int userId, CreateWorkoutPlanRequest request)
        {
            var plan = new WorkoutPlan
            {
                Name = request.Name,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                PlanExercises = request.ExerciseIds.Select(id => new WorkoutPlanExercise { ExerciseId = id }).ToList()
            };

            _context.WorkoutPlans.Add(plan);
            await _context.SaveChangesAsync();

            return await GetPlanByIdAsync(plan.Id);
        }

        public async Task<List<WorkoutPlanDto>> GetMyWorkoutsAsync(int userId)
        {
            var plans = await _context.WorkoutPlans
                .Include(p => p.PlanExercises)
                    .ThenInclude(pe => pe.Exercise)
                .Include(p => p.Sessions)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return plans.Select(plan => new WorkoutPlanDto
            {
                Id = plan.Id,
                Name = plan.Name,
                CreatedAt = plan.CreatedAt,
                UserId = userId,
                Progress = 0.0,
                SessionsCount = plan.Sessions.Count,
                Exercises = plan.PlanExercises.Select(pe => new ExerciseSessionDto
                {
                    ExerciseId = pe.Exercise.Id,
                    Name = pe.Exercise.Name,
                    TargetMuscles = (pe.Exercise.TargetMuscles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Equipments = (pe.Exercise.Equipment ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    ImageUrl = pe.Exercise.ImageUrl ?? "",
                    VideoUrl = pe.Exercise.VideoUrl ?? "",
                    ExerciseType = pe.Exercise.ExerciseType,
                    Completed = false,
                    Sets = new List<WorkoutSetDto>(),
                    CompletionPercentage = 0.0
                }).ToList()
            }).ToList();
        }

        private async Task<WorkoutPlanDto> GetPlanByIdAsync(int planId)
        {
            var plan = await _context.WorkoutPlans
                .Include(p => p.PlanExercises)
                    .ThenInclude(pe => pe.Exercise)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null) return null;

            return new WorkoutPlanDto
            {
                Id = plan.Id,
                Name = plan.Name,
                CreatedAt = plan.CreatedAt,
                UserId = plan.UserId,
                Exercises = plan.PlanExercises.Select(pe => new ExerciseSessionDto
                {
                    ExerciseId = pe.Exercise.Id,
                    Name = pe.Exercise.Name,
                    TargetMuscles = (pe.Exercise.TargetMuscles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Equipments = (pe.Exercise.Equipment ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    ImageUrl = pe.Exercise.ImageUrl ?? "",
                    VideoUrl = pe.Exercise.VideoUrl ?? "",
                    ExerciseType = pe.Exercise.ExerciseType,
                    Completed = false,
                    Sets = new List<WorkoutSetDto>(),
                    CompletionPercentage = 0.0
                }).ToList()
            };
        }
        public async Task<WorkoutSessionDto> SaveWorkoutSessionAsync(int userId, SaveSessionRequest request)
        {
            // 1. Create the session header
            var session = new WorkoutSession
            {
                UserId = userId,
                WorkoutPlanId = request.WorkoutPlanId,
                IsCompleted = true
            };

            _context.WorkoutSessions.Add(session);
            await _context.SaveChangesAsync(); // Save to get the session.Id

            double totalVolume = 0;
            int totalRepsOverall = 0;

            // 2. Loop through exercises and sets sent by Flutter
            foreach (var reqEx in request.Exercises)
            {
                int setNumber = 1;
                foreach (var setDto in reqEx.Sets)
                {
                    var newSet = new WorkoutSet
                    {
                        ExerciseId = reqEx.ExerciseId,
                        WorkoutSessionId = session.Id,
                        SetNumber = setNumber++,
                        Reps = setDto.Reps,
                        Weight = setDto.Weight,
                        IsCompleted = setDto.IsCompleted
                    };

                    // Only calculate volume for completed sets
                    if (setDto.IsCompleted)
                    {
                        totalRepsOverall += setDto.Reps;
                        if (setDto.Weight.HasValue)
                        {
                            totalVolume += (double)(setDto.Weight.Value * setDto.Reps);
                        }
                    }

                    _context.WorkoutSets.Add(newSet);
                }
            }

            // 3. Create the Analysis Record
            var analysis = new WorkoutAnalysis
            {
                SessionId = session.Id,
                TotalVolume = totalVolume,
                TotalReps = totalRepsOverall,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkoutAnalyses.Add(analysis);
            await _context.SaveChangesAsync();

            // 4. Fetch the plan name for the response DTO
            var planName = await _context.WorkoutPlans
                .Where(p => p.Id == request.WorkoutPlanId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync() ?? "Custom Workout";

            // 5. Return the mapped DTO
            return await MapSessionToDtoAsync(session.Id, request.WorkoutPlanId, planName, totalVolume, userId, request);
        }

        // Helper method to keep SaveWorkoutSessionAsync clean
        private async Task<WorkoutSessionDto> MapSessionToDtoAsync(int sessionId, int planId, string planName, double totalVolume, int userId, SaveSessionRequest request)
        {
            // 1. Setup the base Session DTO
            var sessionDto = new WorkoutSessionDto
            {
                Id = sessionId,
                WorkoutId = planId,
                WorkoutName = planName,
                PerformedAt = DateTime.UtcNow,
                UserId = userId,
                TotalVolume = totalVolume,
                Progress = 1.0, // Mark as 100% since it's saved
                Exercises = new List<ExerciseSessionDto>()
            };

            // 2. Fetch all required exercises in ONE database call (Fixes the N+1 issue)
            var exerciseIds = request.Exercises.Select(e => e.ExerciseId).ToList();
            var exercisesDict = await _context.Exercises
                .Where(e => exerciseIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id);

            // 3. Loop through the request exercises and map them
            foreach (var reqEx in request.Exercises)
            {
                // Grab the exercise from our memory dictionary instead of hitting the DB again
                if (!exercisesDict.TryGetValue(reqEx.ExerciseId, out var exercise))
                {
                    continue; // Skip if the exercise ID somehow doesn't exist in the DB
                }

                int totalSets = reqEx.Sets?.Count ?? 0;
                int completedSets = reqEx.Sets?.Count(s => s.IsCompleted) ?? 0;

                sessionDto.Exercises.Add(new ExerciseSessionDto
                {
                    ExerciseId = exercise.Id,
                    Name = exercise.Name,

                    // Safely handle null or empty strings before splitting
                    TargetMuscles = string.IsNullOrWhiteSpace(exercise.TargetMuscles)
                        ? new List<string>()
                        : exercise.TargetMuscles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),

                    Equipments = string.IsNullOrWhiteSpace(exercise.Equipment)
                        ? new List<string>()
                        : exercise.Equipment.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),

                    ImageUrl = exercise.ImageUrl ?? "",
                    VideoUrl = exercise.VideoUrl ?? "",
                    ExerciseType = exercise.ExerciseType,

                    Completed = totalSets > 0 && completedSets == totalSets,
                    CompletionPercentage = totalSets == 0 ? 0 : (double)completedSets / totalSets,
                    Sets = reqEx.Sets ?? new List<WorkoutSetDto>()
                });
            }

            return sessionDto;
        }

        public async Task<ProgressOverviewDto> GetProgressOverviewAsync(int userId)
        {
            // 1. Fetch Sessions with Sets, Exercises, and Analysis
            // We filter by IsCompleted to ensure we only show finished workouts in progress
            var sessions = await _context.WorkoutSessions
                .Where(s => s.UserId == userId && s.IsCompleted)
                .Include(s => s.WorkoutSets)
                    .ThenInclude(set => set.Exercise)
                .Include(s => s.WorkoutAnalysis)
                .Include(s => s.WorkoutPlan)
                .ToListAsync();

            // 2. Fetch Plans to calculate "Saved Workouts" and "Exercises Inside Plans"
            var plans = await _context.WorkoutPlans
                .Where(p => p.UserId == userId)
                .Include(p => p.PlanExercises)
                .ToListAsync();

            // If no sessions exist, return a clean DTO with count of saved plans
            if (!sessions.Any())
            {
                return new ProgressOverviewDto
                {
                    SavedWorkouts = plans.Count,
                    ExercisesInsidePlans = plans.Sum(p => p.PlanExercises.Count),
                    VolumeTrend = new List<VolumeTrendDto>()
                };
            }

            // 3. Logic for AvgWeeklyVolume: Calculate weeks since the first workout
            var firstSessionDate = sessions.Min(s => s.StartTime);
            var totalDaysActive = (DateTime.UtcNow - firstSessionDate).TotalDays;
            // We use Math.Max(1, Math.Ceiling(...)) so that Day 1 counts as 1 full week
            double totalWeeksCount = Math.Max(1, Math.Ceiling(totalDaysActive / 7.0));

            // 4. Top Volume Exercise calculation
            // Groups all sets by Exercise Name to find the one with the highest calculated volume
            var topExercise = sessions.SelectMany(s => s.WorkoutSets)
                .GroupBy(set => set.Exercise.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    TotalExVolume = g.Sum(set => (double)(set.Weight ?? 0) * set.Reps)
                })
                .OrderByDescending(x => x.TotalExVolume)
                .FirstOrDefault();

            // 5. Top Workout calculation
            // Uses the Plan Name linked to the Session
            var topWorkout = sessions
                .Where(s => s.WorkoutPlan != null)
                .GroupBy(s => s.WorkoutPlan!.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    TotalPlanVolume = g.Sum(s => s.WorkoutAnalysis?.TotalVolume ?? 0)
                })
                .OrderByDescending(x => x.TotalPlanVolume)
                .FirstOrDefault();

            // 6. Overall Completion (Percentage of sets marked as completed)
            double totalSetsCount = sessions.SelectMany(s => s.WorkoutSets).Count();
            double completedSetsCount = sessions.SelectMany(s => s.WorkoutSets).Count(set => set.IsCompleted);

            // 7. Final DTO Assembly
            return new ProgressOverviewDto
            {
                OverallCompletion = totalSetsCount == 0 ? 0 : (completedSetsCount / totalSetsCount) * 100,
                TotalVolume = sessions.Sum(s => s.WorkoutAnalysis?.TotalVolume ?? 0),
                AvgSessionVolume = sessions.Average(s => s.WorkoutAnalysis?.TotalVolume ?? 0),

                // Volume and Session counts for the last 7 rolling days
                SessionsThisWeek = sessions.Count(s => s.StartTime >= DateTime.UtcNow.AddDays(-7)),
                WeeklyVolume = sessions.Where(s => s.StartTime >= DateTime.UtcNow.AddDays(-7))
                                       .Sum(s => s.WorkoutAnalysis?.TotalVolume ?? 0),

                // Fixed: Total lifetime volume divided by the number of active weeks
                AvgWeeklyVolume = sessions.Sum(s => s.WorkoutAnalysis?.TotalVolume ?? 0) / totalWeeksCount,

                TopVolumeExercise = topExercise?.Name ?? "None",
                TopWorkout = topWorkout?.Name ?? "None",

                SavedWorkouts = plans.Count,
                ExercisesInsidePlans = plans.Sum(p => p.PlanExercises.Count),

                // Provides data for the Flutter Chart (Last 7 sessions)
                VolumeTrend = sessions
                    .OrderBy(s => s.StartTime)
                    .TakeLast(7)
                    .Select(s => new VolumeTrendDto
                    {
                        DateLabel = s.StartTime.ToString("MMM dd"),
                        Volume = s.WorkoutAnalysis?.TotalVolume ?? 0
                    }).ToList()
            };
        }
        public async Task<bool> DeleteWorkoutPlanAsync(int planId, int userId)
        {
            // 1. Find the plan, ensuring it belongs to the correct user
            var plan = await _context.WorkoutPlans
                .Include(p => p.PlanExercises) // Bring in the join table records
                .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId);

            if (plan == null)
            {
                return false; // Plan not found or doesn't belong to this user
            }

            // 2. Protect the user's history!
            // Find all past sessions that used this plan template
            var pastSessions = await _context.WorkoutSessions
                .Where(s => s.WorkoutPlanId == planId)
                .ToListAsync();

            // Detach them from the plan so they aren't deleted by cascade
            foreach (var session in pastSessions)
            {
                session.WorkoutPlanId = null;
            }

            // 3. Delete the plan
            // EF Core will automatically delete the linked PlanExercises (the join table)
            _context.WorkoutPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
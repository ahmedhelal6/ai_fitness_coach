using Ai_Fitness_Coach.DTOs;
using Ai_Fitness_Coach.Models;

namespace Ai_Fitness_Coach.Services
{
    public interface IMealService
    {
        public Task<MealPlanResponseDto> GenerateFullDayMealPlanAsync(User user);
    }
}

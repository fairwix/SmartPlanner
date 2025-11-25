using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartPlanner.Domain.Entities
{
    public class Goal : BaseEntity
    {
        // ПЕРЕНЕСЕНО ИЗ ProjectState.Messages (преобразовано)
        public string Title { get; set; } = string.Empty;        // Бывшее "message"
        public string Description { get; set; } = string.Empty;
        
        // НОВЫЕ ПОЛЯ для расширенной функциональности
        public GoalCategory Category { get; set; }
        public GoalPriority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public int TargetValue { get; set; } = 1;
        public int CurrentValue { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public bool IsAiGenerated { get; set; } = false;
        
        // ПЕРЕНЕСЕНО ИЗ HomeController.HandleAction (награда за действие)
        public int RewardAmount { get; set; } = 10;             // Было: +10 coins за сообщение
        
        // Внешние ключи
        public Guid UserId { get; set; }
        
        // Навигационные свойства
        public virtual User User { get; set; } = null!;
        public virtual List<GoalProgress> ProgressHistory { get; set; } = new List<GoalProgress>();

        // ВЫЧИСЛЯЕМЫЕ СВОЙСТВА
        public double ProgressPercentage 
        { 
            get 
            { 
                return TargetValue > 0 ? (CurrentValue * 100.0) / TargetValue : 0; 
            } 
        }

        public int RemainingValue => TargetValue - CurrentValue;

        // ПЕРЕНЕСЕНО ИЗ HomeController.HandleAction + расширено
        public void UpdateProgress(int value)
        {
            var oldValue = CurrentValue;
            CurrentValue = Math.Min(value, TargetValue);
            
            // Записываем в историю прогресса
            ProgressHistory.Add(new GoalProgress
            {
                Id = Guid.NewGuid(),
                GoalId = this.Id,
                Value = CurrentValue,
                PreviousValue = oldValue,
                CreatedAt = DateTime.UtcNow
            });

            // Проверяем завершение (аналог старой логики изменения состояния)
            if (CurrentValue >= TargetValue && !IsCompleted)
            {
                CompleteGoal();
            }
            
            UpdatedAt = DateTime.UtcNow;
        }

        // ПЕРЕНЕСЕНО ИЗ HomeController.HandleAction (начисление награды)
        private void CompleteGoal()
        {
            IsCompleted = true;
            // Было: _state.UserBalance += 10;
            User?.AddReward(RewardAmount);
        }

        // ПЕРЕНЕСЕНО ИЗ HomeController.HandleAction (валидация)
        public bool IsValid()
        {
            // Было: if (string.IsNullOrWhiteSpace(message)) return false;
            if (string.IsNullOrWhiteSpace(Title)) 
                return false;
                
            // Было: if (message.Length > 500) return false;
            if (Title.Length > 500) 
                return false;
                
            return true;
        }

        // НОВАЯ БИЗНЕС-ЛОГИКА
        public bool IsExpired() => DueDate < DateTime.UtcNow;
        
        public bool CanBeEdited() => !IsCompleted && !IsExpired();
        
        public bool IsOnTrack()
        {
            if (IsCompleted || DueDate == DateTime.MinValue) 
                return true;
                
            var timePassed = (DateTime.UtcNow - CreatedAt).TotalDays;
            var totalTime = (DueDate - CreatedAt).TotalDays;
            var expectedProgress = timePassed / totalTime;
            
            return ProgressPercentage >= expectedProgress * 100;
        }
    }
}
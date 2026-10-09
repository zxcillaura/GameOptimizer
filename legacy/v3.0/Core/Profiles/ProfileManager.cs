using System;
using System.Collections.Generic;
using System.Linq;

namespace GameOptimizer.Core.Profiles
{
    /// <summary>
    /// Профиль оптимизации - набор предкonfigurированных твиков
    /// </summary>
    public class GameProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public List<string> IncludedTweakIds { get; set; } = new List<string>();
        public bool IsBuiltIn { get; set; } = true;
    }

    /// <summary>
    /// Менеджер профилей оптимизации
    /// </summary>
    public class ProfileManager
    {
        private static ProfileManager _instance;
        private List<GameProfile> _profiles;

        public static ProfileManager Instance => _instance ?? (_instance = new ProfileManager());

        public IReadOnlyList<GameProfile> Profiles => _profiles.AsReadOnly();

        public ProfileManager()
        {
            InitializeDefaultProfiles();
        }

        private void InitializeDefaultProfiles()
        {
            _profiles = new List<GameProfile>
            {
                // Профиль для конкурентных игр (CS:GO, Valorant, Apex Legends)
                new GameProfile
                {
                    Id = "competitive",
                    Name = "Конкурентные игры",
                    Description = "Максимум FPS и минимум пинга для CS:GO, Valorant, Apex",
                    Icon = "🎯",
                    IsBuiltIn = true,
                    IncludedTweakIds = new List<string>
                    {
                        "MPO_Disable",
                        "TCP_NoDelay",
                        "Network_Throttling",
                        "Disable_Telemetry",
                        "GPU_Context_Priority",
                        "Disable_Fullscreen_Optimizations",
                        "DisableSearchIndexer",
                        "DisableWindowsUpdate",
                        "ReduceWindowsAnimations"
                    }
                },

                // Профиль для одиночных игр (AAA-тайтлы)
                new GameProfile
                {
                    Id = "singleplayer",
                    Name = "Одиночные игры",
                    Description = "Баланс между графикой и производительностью для AAA-тайтлов",
                    Icon = "🎮",
                    IsBuiltIn = true,
                    IncludedTweakIds = new List<string>
                    {
                        "MPO_Disable",
                        "Disable_Telemetry",
                        "GPU_Context_Priority",
                        "DisableSearchIndexer",
                        "ReduceWindowsAnimations",
                        "EnableHighPerformanceMode"
                    }
                },

                // Профиль для стриминга
                new GameProfile
                {
                    Id = "streaming",
                    Name = "Стриминг (Twitch/YouTube)",
                    Description = "Оптимизация CPU для одновременной игры и трансляции",
                    Icon = "📡",
                    IsBuiltIn = true,
                    IncludedTweakIds = new List<string>
                    {
                        "MPO_Disable",
                        "TCP_NoDelay",
                        "Network_Throttling",
                        "Disable_Telemetry",
                        "GPU_Context_Priority",
                        "EnableHighPerformanceMode",
                        "DisableSearchIndexer",
                        "ReduceWindowsAnimations"
                    }
                },

                // Профиль для очистки системы
                new GameProfile
                {
                    Id = "cleanup",
                    Name = "Очистка системы",
                    Description = "Удаление мусора и ненужных сервисов",
                    Icon = "🧹",
                    IsBuiltIn = true,
                    IncludedTweakIds = new List<string>
                    {
                        "DisableSearchIndexer",
                        "DisableWindowsUpdate",
                        "DisableTaskScheduler",
                        "ClearTempFiles",
                        "DisableNotifications"
                    }
                },

                // Кастомный профиль
                new GameProfile
                {
                    Id = "custom",
                    Name = "Кастомный",
                    Description = "Выберите твики вручную для максимальной гибкости",
                    Icon = "⚙️",
                    IsBuiltIn = true,
                    IncludedTweakIds = new List<string>()
                }
            };
        }

        public GameProfile GetProfile(string profileId)
        {
            return _profiles.FirstOrDefault(p => p.Id == profileId);
        }

        public GameProfile GetProfileByName(string name)
        {
            return _profiles.FirstOrDefault(p => p.Name == name);
        }

        public void AddProfile(GameProfile profile)
        {
            if (_profiles.Any(p => p.Id == profile.Id))
                throw new InvalidOperationException($"Профиль с ID '{profile.Id}' уже существует");

            profile.IsBuiltIn = false;
            _profiles.Add(profile);
        }

        public void RemoveProfile(string profileId)
        {
            var profile = GetProfile(profileId);
            if (profile != null && !profile.IsBuiltIn)
            {
                _profiles.Remove(profile);
            }
        }

        public void UpdateProfile(GameProfile profile)
        {
            var existing = GetProfile(profile.Id);
            if (existing != null)
            {
                existing.Name = profile.Name;
                existing.Description = profile.Description;
                existing.IncludedTweakIds = profile.IncludedTweakIds;
            }
        }

        public List<string> GetProfileTweaks(string profileId)
        {
            var profile = GetProfile(profileId);
            return profile?.IncludedTweakIds ?? new List<string>();
        }
    }
}

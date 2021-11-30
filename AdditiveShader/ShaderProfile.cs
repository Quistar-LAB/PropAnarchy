using System;
using System.Globalization;

namespace PropAnarchy.AdditiveShader {
    /// <summary>
    /// <para>This class is used to extract and parse parameters from a mesh name.</para>
    /// <para>
    /// It also derives whether the shader stays on across the midnight boundary,
    /// and whether it is always on.
    /// </para>
    /// </summary>
    public readonly struct ShaderProfile {
        // Hard-coded values derived from SimulationManager's `SUNRISE_HOUR` and `SUNSET_HOUR` members.
        // This is done because mods such as Real Time can alter those vanilla values. We need the
        // original values as that's what most asset authors base their twilight shader on/off times on.
        private const float SUNRISE = 5f;
        private const float SUNSET = 20f;

        // A 1-hour boundary around the sunrise/sunset times. This is because asset authors tend to use
        // +/- 1 hour (of sunrise/sunset) for their on/off times. We can bucket all the 'twilight'
        // shader assets and spread them around the day/night transitions.
        private const float ONE_HOUR = 1.1f;
        private const float SUNRISE_START = SUNRISE - ONE_HOUR;
        private const float SUNRISE_END = SUNRISE + ONE_HOUR;
        private const float SUNSET_START = SUNSET - ONE_HOUR;
        private const float SUNSET_END = SUNSET + ONE_HOUR;

        // If tag present, prevents identification as a twilight-toggled shader.
        private const string NOT_TWILIGHT = "not-twilight";
        private const string ALWAYSON = "AlwaysOn";
        private const string MODDED = "Modded";
        private const string DAYTIME = "DayTime";
        private const string NIGHTTIME = "NightTime";
        private const string CONTAINER = "Container";

        [Flags]
        public enum Profiles : ushort {
            PROFILE_TYPE = 0xff00,
            RC = 0x0001,
            ALWAYSON = 0X0002,
            STATIC = 0x0004,
            OVERLAPMIDNIGHT = 0x0008,
            TOGGLEDBYTWILIGHT = 0x0010,
            DAYTIMEONLY = 0x0020,
            NIGHTTIMEONLY = 0x0040,
            AlwaysOn = 0x0100 | ALWAYSON | STATIC,
            Modded = 0x0200 | RC | STATIC,
            DayTime = 0x0400 | TOGGLEDBYTWILIGHT | DAYTIMEONLY,
            NightTime = 0x0800 | OVERLAPMIDNIGHT | TOGGLEDBYTWILIGHT | NIGHTTIMEONLY,
            Container = 0x1000 | ALWAYSON,
            OldRonyxProfile = 0x2000 // This is used for old Ronyx69 additive shader compatible assets
        }

        public readonly Profiles m_profile;
        //  Profiles      On       Off      RC     Always Static Midnt  Twilt  Day    Night
        //{ "AlwaysOn"  , 0f     , 24f    , false, true , true , false, false, false, false },
        //{ "Modded"    , -1     , -1     , true , false, true , false, false, false, false },
        //{ "DayTime"   , SUNRISE, SUNSET , false, false, false, false, true , true , false },
        //{ "NightTime" , SUNSET , SUNRISE, false, false, false, true , true , false, true  },
        //{ "Container" , 0f     , 24f    , false, true , false, false, false, false, false }, // AssetType.Container
        public ShaderProfile(string rawMeshName) {
            char[] DELIMITERS = { ' ' };
            try {
                string[] tags = rawMeshName.Split(DELIMITERS, StringSplitOptions.RemoveEmptyEntries);
                // New AdditiveShader Tags [ KEYWORD FADE INTENSITY ]
                switch (tags[1]) {
                case ALWAYSON:
                    OnTime = 0f;
                    OffTime = 24f;
                    m_profile = Profiles.AlwaysOn;
                    Fade = float.Parse(tags[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    Intensity = float.Parse(tags[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                    break;
                case MODDED:
                    OnTime = -1f;
                    OffTime = -1f;
                    m_profile = Profiles.Modded;
                    Fade = float.Parse(tags[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    Intensity = float.Parse(tags[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                    break;
                case DAYTIME:
                    OnTime = SUNRISE;
                    OffTime = SUNSET;
                    m_profile = Profiles.DayTime;
                    Fade = float.Parse(tags[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    Intensity = float.Parse(tags[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                    break;
                case NIGHTTIME:
                    OnTime = SUNSET;
                    OffTime = SUNRISE;
                    m_profile = Profiles.NightTime;
                    Fade = float.Parse(tags[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    Intensity = float.Parse(tags[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                    break;
                case CONTAINER:
                    OnTime = 0f;
                    OffTime = 24f;
                    m_profile = Profiles.Container;
                    Fade = float.Parse(tags[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    Intensity = float.Parse(tags[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                    break;
                default: // For parsing old Additive Shader tags [ ON - OFF - FADE - INTENSITY ]
                    OnTime = float.Parse(tags[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                    OffTime = float.Parse(tags[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    Fade = float.Parse(tags[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                    Intensity = float.Parse(tags[4], NumberStyles.Float, CultureInfo.InvariantCulture);
                    m_profile = Profiles.OldRonyxProfile;
                    break;
                }
            } catch (Exception e) {
                throw new FormatException($"Invalid mesh name format: {rawMeshName}", e);
            }
        }

        /// <summary>
        /// <para>Gets a value defining the game time at which shader is shown.</para>
        /// <para>Note: Will be negative if <c>AlwaysOff</c> keyword was used.</para>
        /// </summary>
        public float OnTime { get; }

        /// <summary>
        /// Gets a value defining the game time at which shader is hidden.
        /// </summary>
        public float OffTime { get; }

        /// <summary>
        /// <para>Gets a value which controls fading of the additive shader.</para>
        /// <para>
        /// The additive shader decreases opacity as it gets closer to other objects.
        /// Higher value means less fading, because reasons.
        /// </para>
        /// </summary>
        public float Fade { get; }

        /// <summary>
        /// <para>Gets a value indicating the light intensity multiplier to apply to the additive shader.</para>
        /// <para>Values above 1 may start to bloom.</para>
        /// </summary>
        public float Intensity { get; }

        /// <summary>
        /// Gets a value indicating whether OnTime > OffTime (ie. the shader is visible across the midnight boundary).
        /// </summary>
        public bool OverlapsMidnight => (m_profile & Profiles.OVERLAPMIDNIGHT) == Profiles.OVERLAPMIDNIGHT;

        /// <summary>
        /// <para>Gets a value indicating whether the shader is toggled at dusk/dawn.</para>
        /// <para>One of <see cref="IsDayTimeOnly"/> or <see cref="IsNightTimeOnly"/> will be <c>true</c>.</para>
        /// </summary>
        public bool IsToggledByTwilight => (m_profile & Profiles.TOGGLEDBYTWILIGHT) == Profiles.TOGGLEDBYTWILIGHT;

        /// <summary>
        /// <para>Gets a value indicating whether the shader is on all day _and_ off all night.</para>
        /// <para>Note: This is determined by the <c>DayTime</c> keyword, not on/off times.</para>
        /// </summary>
        public bool IsDayTimeOnly => (m_profile & Profiles.DAYTIMEONLY) == Profiles.DAYTIMEONLY;

        /// <summary>
        /// <para>Gets a value indicating whether the shader is on all night _and_ off all day.</para>
        /// <para>Note: This is determined by either the <c>NightTime</c> keyword, or on/off times which occur during twilight.</para>
        /// </summary>
        public bool IsNightTimeOnly => (m_profile & Profiles.NIGHTTIMEONLY) == Profiles.NIGHTTIMEONLY;

        /// <summary>
        /// Gets a value indicating whether the shader is permanently visible.
        /// </summary>
        public bool IsAlwaysOn => (m_profile & Profiles.ALWAYSON) == Profiles.ALWAYSON;

        /// <summary>
        /// Gets a value indicating whether the shader is remotely controlled by other mods.
        /// </summary>
        public bool IsRemotelyControlled => (m_profile & Profiles.RC) == Profiles.RC;

        /// <summary>
        /// <para>Gets a value indicating whether the shader is static (always on, or always off).</para>
        /// <para>Note: If <c>true</c>, and <see cref="IsAlwaysOn"/> is <c>false</c>, it means "always off".</para>
        /// </summary>
        public bool IsStatic => (m_profile & Profiles.STATIC) == Profiles.STATIC;

        private static bool IsDuringSunrise(float time) => SUNRISE_START < time && time < SUNRISE_END;

        private static bool IsDuringSunset(float time) => SUNSET_START < time && time < SUNSET_END;
    }
}

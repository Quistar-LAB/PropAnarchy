using System;

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
        //private const float ONE_HOUR = 1.1f;
        //private const float SUNRISE_START = SUNRISE - ONE_HOUR;
        //private const float SUNRISE_END = SUNRISE + ONE_HOUR;
        //private const float SUNSET_START = SUNSET - ONE_HOUR;
        //private const float SUNSET_END = SUNSET + ONE_HOUR;

        // If tag present, prevents identification as a twilight-toggled shader.
        //private const string NOT_TWILIGHT = "not-twilight";
        //private const string ALWAYSON = "AlwaysOn";
        //private const string MODDED = "Modded";
        //private const string DAYTIME = "DayTime";
        //private const string NIGHTTIME = "NightTime";
        //private const string CONTAINER = "Container";
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
            int start = 15;
            int i = start;
            if (rawMeshName[i] >= '0' && rawMeshName[i] <= '9') {
                m_profile = Profiles.OldRonyxProfile;
                while (rawMeshName[i++] != ' ') ;
                OnTime = ParseFloat(rawMeshName, start, i - 1);
                start = i;
                while (rawMeshName[i++] != ' ') ;
                OffTime = ParseFloat(rawMeshName, start, i - 1);
                start = i;
                while (rawMeshName[i++] != ' ') ;
                Fade = ParseFloat(rawMeshName, start, i - 1);
                start = i;
                Intensity = ParseFloat(rawMeshName, start, rawMeshName.Length);
            } else {
                if (ParseAlwaysOn(rawMeshName, i)) {
                    m_profile = Profiles.AlwaysOn;
                    OnTime = 0f;
                    OffTime = 24f;
                    while (rawMeshName[++i] != ' ') ;
                    start = ++i;
                    while (rawMeshName[i++] != ' ') ;
                    Fade = ParseFloat(rawMeshName, start, i - 1);
                    start = i;
                    Intensity = ParseFloat(rawMeshName, start, rawMeshName.Length);
                } else if (ParseModded(rawMeshName, i)) {
                    m_profile = Profiles.Modded;
                    OnTime = -1f;
                    OffTime = -1f;
                    while (rawMeshName[++i] != ' ') ;
                    start = ++i;
                    while (rawMeshName[i++] != ' ') ;
                    Fade = ParseFloat(rawMeshName, start, i - 1);
                    start = i;
                    Intensity = ParseFloat(rawMeshName, start, rawMeshName.Length);
                } else if (ParseDayTime(rawMeshName, i)) {
                    m_profile = Profiles.DayTime;
                    OnTime = SUNRISE;
                    OffTime = SUNSET;
                    while (rawMeshName[++i] != ' ') ;
                    start = ++i;
                    while (rawMeshName[i++] != ' ') ;
                    Fade = ParseFloat(rawMeshName, start, i - 1);
                    start = i;
                    Intensity = ParseFloat(rawMeshName, start, rawMeshName.Length);
                } else if (ParseNightTime(rawMeshName, i)) {
                    m_profile = Profiles.NightTime;
                    OnTime = SUNSET;
                    OffTime = SUNRISE;
                    while (rawMeshName[++i] != ' ') ;
                    start = ++i;
                    while (rawMeshName[i++] != ' ') ;
                    Fade = ParseFloat(rawMeshName, start, i - 1);
                    start = i;
                    Intensity = ParseFloat(rawMeshName, start, rawMeshName.Length);
                } else if (ParseContainer(rawMeshName, i)) {
                    m_profile = Profiles.Container;
                    OnTime = 0f;
                    OffTime = 24f;
                    while (rawMeshName[++i] != ' ') ;
                    start = ++i;
                    while (rawMeshName[i++] != ' ') ;
                    Fade = ParseFloat(rawMeshName, start, i - 1);
                    start = i;
                    Intensity = ParseFloat(rawMeshName, start, rawMeshName.Length);
                } else {
                    m_profile = default;
                    OnTime = 0f;
                    OffTime = 0f;
                    Fade = 0f;
                    Intensity = 0f;
                }
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

        private static float ParseFloat(string s, int begin, int end) {
            float result = 0f;
            char c = s[begin];
            int sign, start = begin;

            if (c == '-') {
                sign = -1;
                start = begin + 1;
            } else if (c > 57 || c < 48) {
                do {
                    ++start;
                    c = s[start];
                } while (start < end);
                if (start >= end) {
                    return float.NaN;
                }
                if (c == '-') {
                    sign = -1;
                    ++start;
                } else {
                    sign = 1;
                }
            } else {
                start = begin + 1;
                result = 10 * result + (c - 48);
                sign = 1;
            }

            int i = start;
            for (; i < end; ++i) {
                c = s[i];
                if (c > 57 || c < 48) {
                    if (c == '.') {
                        ++i;
                        goto DecimalPoint;
                    } else {
                        return float.NaN;
                    }
                }

                result = 10 * result + (c - 48);
            }
            return result * sign;

DecimalPoint:
            long temp = 0;
            int length = i;
            float exponent = 0f;

            for (; i < end; ++i) {
                c = s[i];
                if (c > 57 || c < 48) {
                    length = i - length;
                    goto ProcessFraction;
                }
                temp = 10 * temp + (c - 48);
            }
            length = i - length;

ProcessFraction:
            float fraction = temp;
            if (length < powLookup.Length) {
                fraction /= powLookup[length];
            } else {
                fraction /= powLookup[powLookup.Length - 1];
            }
            result += fraction;
            result *= sign;
            if (exponent > 0) {
                result *= exponent;
            } else if (exponent < 0) {
                result /= -exponent;
            }
            return result;
        }

        private static readonly int[] powLookup = new[] {
            1, // 10^0
            10, // 10^1
            100, // 10^2
            1000, // 10^3
            10000 // 10^4
        };

        private static bool ParseAlwaysOn(string data, int i) => data[i] == 'A' && data[i + 1] == 'l' && data[i + 2] == 'w' &&
                                                                 data[i + 3] == 'a' && data[i + 4] == 'y' && data[i + 5] == 's' &&
                                                                 data[i + 6] == 'O' && data[i + 7] == 'n';

        private static bool ParseModded(string data, int i) => data[i] == 'M' && data[i + 1] == 'o' && data[i + 2] == 'd' &&
                                                               data[i + 3] == 'd' && data[i + 4] == 'e' && data[i + 5] == 'd';

        private static bool ParseDayTime(string data, int i) => data[i] == 'D' && data[i + 1] == 'a' && data[i + 2] == 'y' &&
                                                                data[i + 3] == 'T' && data[i + 4] == 'i' && data[i + 5] == 'm' &&
                                                                data[i + 6] == 'e';

        private static bool ParseNightTime(string data, int i) => data[i] == 'N' && data[i + 1] == 'i' && data[i + 2] == 'g' &&
                                                                  data[i + 3] == 'h' && data[i + 4] == 't' && data[i + 5] == 'T' &&
                                                                  data[i + 6] == 'i' && data[i + 7] == 'm' && data[i + 8] == 'e';

        private static bool ParseContainer(string data, int i) => data[i] == 'C' && data[i + 1] == 'o' && data[i + 2] == 'n' &&
                                                                  data[i + 3] == 't' && data[i + 4] == 'a' && data[i + 5] == 'i' &&
                                                                  data[i + 6] == 'n' && data[i + 7] == 'e' && data[i + 8] == 'r';
    }
}

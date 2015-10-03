using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Fireteams.Common.Models
{
    public enum Language
    {
        English,
        Dansk,      //Danish
        Deutsch,    //German
        Español,    //Spanish
        Français,   //French
        Italiano,   //Italian
        Nederlands, //Dutch
        Norsk,      //Norwegian
        Polski,     //Polish
        Português,  //Portugese
        Pyccкий,    //Russian
        Suomi,      //Finnish
        Svenska,    //Swedish
        Türkçe,     //Turkish
        العربية,    //Arabic
        日本語,      //Japanese
        한국어,      //Korean
        普通话       //Chinese
    }

    public enum Platform
    {
        [Display(Name="PlayStation 3")]
        PS3,

        [Display(Name="PlayStation 4")]
        PS4,

        [Display(Name="Xbox 360")]
        Xbox360,

        [Display(Name="Xbox One")]
        XboxOne
    }

    public enum Activity
    {
        [Display(Name="Daily Heroic Story")]
        [ActivityLevel(22)]
        DailyStory,

        [Display(Name="Weekly Heroic Strike")]
        [ActivityLevel(26)]
        WeeklyStrike,

        [Display(Name="Weekly Nightfall (🎤)")]
        [ActivityLevel(32)]
        WeeklyNightfall,

        ///////////////////////////////////////////////////

        [Display(Name="Vault of Glass (Normal - 🎤)")]
        [ActivityLevel(26)]
        VaultOfGlass,

        [Display(Name="Vault of Glass (Hard - 🎤)")]
        [ActivityLevel(30)]
        VaultOfGlassHard,

        ///////////////////////////////////////////////////

        [Display(Name="Crota's End (Normal - 🎤)")]
        [ActivityLevel(30)]
        CrotasEnd,

        [Display(Name="Crota's End (Hard - 🎤)")]
        [ActivityLevel(32)]
        CrotasEndHard,

        ///////////////////////////////////////////////////

        [Display(Name="Prison of Elders (Level 32 - 🎤)")]
        [ActivityLevel(32)]
        PrisonOfEldersMed,

        [Display(Name="Prison of Elders (Level 34 - 🎤)")]
        [ActivityLevel(34)]
        PrisonOfEldersHard,

        [Display(Name="Prison of Elders (Level 35 - 🎤)")]
        [ActivityLevel(35)]
        PrisonOfEldersSkolas,

        ///////////////////////////////////////////////////

        [Display(Name="Trials of Osiris (Weekly PvP Event - 🎤)")]
        [ActivityLevel(34)]
        TrialsOfOsiris
    }

    /// <summary>
    /// Describes the recommended level for the activity
    /// </summary>
    public sealed class ActivityLevelAttribute : Attribute, IEquatable<ActivityLevelAttribute>
    {
        public int Level { get; private set; }

        public ActivityLevelAttribute(int level)
        {
            Level = level;
        }

        #region Equals
        public bool Equals(ActivityLevelAttribute other)
        {
            return base.Equals(other) && Level == other.Level;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object.
        /// </summary>
        /// <returns>
        /// true if <paramref name="obj"/> equals the type and value of this instance; otherwise, false.
        /// </returns>
        /// <param name="obj">An <see cref="T:System.Object"/> to compare with this instance or null. </param>
        public override bool Equals(object obj)
        {
            if( ReferenceEquals(null, obj) )
                return false;

            if( ReferenceEquals(this, obj) )
                return true;

            return obj is ActivityLevelAttribute && Equals((ActivityLevelAttribute)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ Level;
            }
        }
        #endregion
    }

    public static class EnumExt
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            return type.GetField(name)
                .GetCustomAttributes(false)
                .OfType<TAttribute>()
                .SingleOrDefault();
        }

        public static int GetUsersNeeded(this Activity activity)
        {
            //tl;dr - Raids need 6, dailies/weeklies/PoE/ToO need 3
            switch( activity )
            {
                case Activity.VaultOfGlass:
                case Activity.VaultOfGlassHard:
                case Activity.CrotasEnd:
                case Activity.CrotasEndHard:
                    return 6;

                default:
                    return 3;
            }
        }

        public static int GetRecommendedLevel(this Activity activity)
        {
            return activity.GetAttribute<ActivityLevelAttribute>().Level;
        }
    }
}
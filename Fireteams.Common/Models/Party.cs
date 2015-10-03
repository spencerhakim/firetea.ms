using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fireteams.Common.Models
{
    public class Party
    {
        [Required]
        [EnumDataType(typeof(Language))]
        public Language Language { get; set; }

        [Required]
        [EnumDataType(typeof(Platform))]
        public Platform Platform { get; set; }

        [Required]
        [EnumDataType(typeof(Activity))]
        public Activity Activity { get; set; }

        [Required, Display(Name="Party Size")]
        [Range(1, 5)]
        public int PartySize { get; set; }

        [Required]
        [RegularExpression(@"[A-Za-z][\w\- ]{0,15}")]
        public string Username { get; set; }

        [Required]
        [Range(20, 32)]
        public int Level { get; set; }

        public void Validate()
        {
            var validRes = new List<ValidationResult>();
            if( !Validator.TryValidateObject(this, new ValidationContext(this, null, null), validRes, true) )
                throw new ArgumentException(validRes[0].ErrorMessage);

            if( !trialsOfOsirisCheck(DateTime.UtcNow) )
                throw new ArgumentException("The Trials of Osiris are not currently open");

            if( PartySize >= Activity.GetUsersNeeded() )
                throw new ArgumentException("Party size is too large for this activity");

            if( Level < Activity.GetRecommendedLevel() - 3 )
                throw new ArgumentException("Level is too low for this activity");
        }

        private bool trialsOfOsirisCheck(DateTime now)
        {
            if( Activity != Activity.TrialsOfOsiris )
                return true;

            now = now.ToUniversalTime();

            if( now.DayOfWeek > DayOfWeek.Tuesday && now.DayOfWeek < DayOfWeek.Friday )
                return false; //Trials aren't even open on Wed/Thurs

            if( now.DayOfWeek == DayOfWeek.Tuesday )
                return now.Hour < 9; //open before 9am UTC Tuesday

            if( now.DayOfWeek == DayOfWeek.Friday )
                return now.Hour >= 17; //open after 5pm UTC Friday

            return true;
        }
    }
}
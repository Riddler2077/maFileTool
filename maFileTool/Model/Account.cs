using Microsoft.Build.Framework;
using Newtonsoft.Json;
using System;

namespace maFileTool.Model
{
    public class Account
    {
        [Column(1)]
        [Required]
        public string Id { get; set; }
        [Column(2)]
        [Required]
        public string NickName { get; set; }
        [Column(3)]
        [Required]
        public string Login { get; set; }
        [Column(4)]
        [Required]
        public string Password { get; set; }
        [Column(5)]
        [Required]
        public string Phone { get; set; }
        [Column(6)]
        [Required]
        public string RevocationCode { get; set; }
        [Column(7)]
        [Required]
        public string AccountId { get; set; }
        [Column(8)]
        [Required]
        public string SteamId { get; set; }
        [Column(9)]
        [Required]
        public string TradeToken { get; set; }
        [Column(10)]
        [Required]
        public string LVL { get; set; }
        [Column(11)]
        [Required]
        public string DateOfBirth { get; set; }
        [Column(12)]
        [Required]
        public string CC { get; set; }
        [Column(13)]
        [Required]
        public string SteamKey { get; set; }
        [Column(14)]
        [Required]
        public string CSGOPrime { get; set; }
        [Column(15)]
        [Required]
        public string TF2Premium { get; set; }
        [Column(16)]
        [Required]
        public string Market { get; set; }
        [Column(17)]
        [Required]
        public string Email { get; set; }
        [Column(18)]
        [Required]
        public string EmailPassword { get; set; }
        [Column(19)]
        [Required]
        public string Name { get; set; }
        [Column(20)]
        [Required]
        public string Surname { get; set; }
        [Column(21)]
        [Required]
        public string BornDate { get; set; }
        [Column(22)]
        [Required]
        public string Gender { get; set; }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class Column : System.Attribute
    {
        public int ColumnIndex { get; set; }


        public Column(int column)
        {
            ColumnIndex = column;
        }
    }
}

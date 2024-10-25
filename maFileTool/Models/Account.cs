using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace maFileTool.Model
{
    public class Account
    {
        [Column(1)]
        [Required]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [Column(2)]
        [Required]
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [Column(3)]
        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [Column(4)]
        [Required]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Column(5)]
        [Required]
        [JsonPropertyName("emailpassword")]
        public string EmailPassword { get; set; } = string.Empty;

        [Column(6)]
        [Required]
        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [Column(7)]
        [Required]
        [JsonPropertyName("revocationcode")]
        public string RevocationCode { get; set; } = string.Empty;
        public Account() { }

        [JsonConstructor]
        [DynamicDependency(nameof(Id))]
        [DynamicDependency(nameof(Login))]
        [DynamicDependency(nameof(Password))]
        [DynamicDependency(nameof(Email))]
        [DynamicDependency(nameof(EmailPassword))]
        [DynamicDependency(nameof(Phone))]
        [DynamicDependency(nameof(RevocationCode))]
        public Account(string Id, string Login, string Password, string Email, string EmailPassword, string Phone, string RevocationCode)
        {
            this.Id = Id;
            this.Login = Login;
            this.Password = Password;
            this.Email = Email;
            this.EmailPassword = EmailPassword;
            this.Phone = Phone;
            this.RevocationCode = RevocationCode;
        }
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

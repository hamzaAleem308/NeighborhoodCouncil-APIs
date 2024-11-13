using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NcDemo.Controllers
{
    public class CodeGenerator
    {
        public static string GenerateJoinCode(int councilId)
        {
            const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            string code = councilId.ToString(); // Start with the memberId as part of the code

            for (int i = 0; i < 6; i++)  // Add 6 random characters to the code
            {
                code += characters[random.Next(characters.Length)];
            }

            return code;
        }
    }
}
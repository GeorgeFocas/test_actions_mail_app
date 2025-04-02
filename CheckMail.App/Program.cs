using System.Text;
using MailKit.Net.Smtp;
using MimeKit;

namespace CheckMail.App
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var mailStuff = new MailStuff("localhost", 3025);
            mailStuff.SendMail(
                "user1@localhost",
                "user2@localhost",
                "Test Mail",
                "This is a test mail"
            );
        }
    }

    public class MailStuff
    {
        private string _mailServer;
        private int _mailPort;
        
        public MailStuff(string mailServer, int mailPort)
        {
            _mailServer = mailServer;
            _mailPort = mailPort;
        }

        public void SendMail(string mailFrom, string mailTo, string mailSubject, string mailBody)
        {
            var messsage = new MimeMessage
            {
                Subject = mailSubject,
                Body = new TextPart("plain") { Text = mailBody }
            };
            messsage.From.Add(new MailboxAddress("User 1", mailFrom));
            messsage.To.Add(new MailboxAddress("User 2",  mailTo));
            
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Connect(_mailServer, _mailPort, false);
                
                
                smtpClient.Send(messsage);
                smtpClient.Disconnect(true);
            }
        }
    }
}
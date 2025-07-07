using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

namespace EmailSenderLibrary
{
    public class EmailSender
    {
        private class EmailSendResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public long TimingMs { get; set; }
            public string ExceptionType { get; set; }
        }

        [SqlFunction(
            DataAccess = DataAccessKind.None,
            FillRowMethodName = "FillRow",
            TableDefinition = "success BIT, message NVARCHAR(MAX), timingMs BIGINT, exceptionType NVARCHAR(200)"
        )]
        public static IEnumerable FrisiaSendMail(
            SqlString smtpHost,
            SqlInt32 smtpPort,
            SqlString smtpUser,
            SqlString smtpPass,
            SqlString from,
            SqlString to,
            SqlString subject,
            SqlString body,
            SqlBoolean enableSsl,
            SqlInt32 timeout
        )
        {
            var result = new EmailSendResult();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ValidateParams(smtpHost, from, to, subject, body);

                using (var client = new SmtpClient(smtpHost.Value, smtpPort.IsNull ? 25 : smtpPort.Value))
                {
                    client.EnableSsl = enableSsl.IsNull ? false : enableSsl.Value;

                    if (!timeout.IsNull && timeout.Value > 0)
                        client.Timeout = timeout.Value;

                    if (!smtpUser.IsNull)
                    {
                        string pass = smtpPass.IsNull ? "" : smtpPass.Value;
                        client.Credentials = new NetworkCredential(smtpUser.Value, pass);
                    }

                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(from.Value);

                        foreach (var address in to.Value
                            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var trimmed = address.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                mail.To.Add(trimmed);
                            }
                        }

                        mail.Subject = subject.Value;
                        mail.Body = body.Value;
                        mail.IsBodyHtml = true;

                        client.Send(mail);
                        result.Success = true;
                        result.Message = "Email sent successfully.";
                    }
                }
            }
            catch (SmtpFailedRecipientsException ex)
            {
                result.Success = false;
                result.Message = "Error: Failed to deliver to one or more recipients. " + ex.Message;
                result.ExceptionType = nameof(SmtpFailedRecipientsException);
            }
            catch (SmtpException ex)
            {
                result.Success = false;
                result.Message = "SMTP Error: " + ex.Message;
                result.ExceptionType = nameof(SmtpException);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Unexpected error when sending email: " + ex.Message;
                result.ExceptionType = ex.GetType().Name;
            }
            finally
            {
                stopwatch.Stop();
                result.TimingMs = stopwatch.ElapsedMilliseconds;
            }

            yield return result;
        }

        public static void FillRow(
            object obj,
            out SqlBoolean success,
            out SqlString message,
            out SqlInt64 timing,
            out SqlString exceptionType
        )
        {
            var result = (EmailSendResult)obj;
            success = result.Success;
            message = result.Message ?? SqlString.Null;
            timing = result.TimingMs;
            exceptionType = result.ExceptionType ?? SqlString.Null;
        }

        private static void ValidateParams(
            SqlString smtpHost, SqlString from, SqlString to, SqlString subject, SqlString body)
        {
            if (smtpHost.IsNull || from.IsNull || to.IsNull || subject.IsNull || body.IsNull)
                throw new ArgumentNullException("smtpHost, from, to, subject, body are mandatory.");
        }
    }
}

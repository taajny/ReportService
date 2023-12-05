using Cipher;
using EmailSender;
using ReportService.Core;
using ReportService.Core.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ReportService
{
    public partial class ReportService : ServiceBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private int _sendHour;
        private int _intervalInMinutes;
        private bool _sendingRaports;
        private Timer _timer;
        private ErrorRepository _errorRepository = new ErrorRepository();
        private ReportRepository _reportRepository = new ReportRepository();
        private Email _email;
        private GenerateHtmlEmail _htmlEmail = new GenerateHtmlEmail();
        private string _emailReciver;
        private StringCipher _stringCipher = new StringCipher("FB8A78B8-5501-49E1-A437-FC21535ADAD2");

        public ReportService()
        {
            InitializeComponent();

            try
            {
                _emailReciver = ConfigurationManager.AppSettings["ReceiverEmail"];
                _sendHour = Convert.ToInt32(ConfigurationManager.AppSettings["SendHour"]);
                _intervalInMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalInMinutes"]);
                _timer = new Timer(_intervalInMinutes * 60000);
                _sendingRaports = Convert.ToBoolean(ConfigurationManager.AppSettings["SendingRaports"]);

                _email = new Email(new EmailParams
                {
                    HostSmtp = ConfigurationManager.AppSettings["HostSmtp"],
                    Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]),
                    EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSsl"]),
                    SenderName = ConfigurationManager.AppSettings["SenderName"],
                    SenderEmail = ConfigurationManager.AppSettings["SenderEmail"],
                    SenderEmailPassword = DecryptEmailSenderPasswor()
                }); 
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private string DecryptEmailSenderPasswor()
        {
            var encryptedPassword = ConfigurationManager.AppSettings["SenderEmailPassword"];

            if (encryptedPassword.StartsWith("encrypt:"))
            {
                encryptedPassword = _stringCipher.Encrypt(encryptedPassword.Replace("encrypt:", ""));

                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                configFile.AppSettings.Settings["SenderEmailPassword"].Value = encryptedPassword;
                configFile.Save();
            }

            return _stringCipher.Decrypt(encryptedPassword);

        }
        protected override void OnStart(string[] args)
        {
            _timer.Elapsed += DoWork;
            _timer.Start();
            Logger.Info("Service started ...");
        } 
        protected override void OnStop()
        {
            Logger.Info("Service stopped ...");
        }
        private async void DoWork(object sender, ElapsedEventArgs e)
        {
            try
            {
                await SendError();
                if(_sendingRaports)
                {
                    await SendReport();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private async Task SendError()
        {
            var errors = _errorRepository.GetErrors(_intervalInMinutes);        

            if( errors == null || !errors.Any() ) 
            {
                return;
            }

            await _email.Send("Błędy w aplikacji", _htmlEmail.GenerateErrors(errors, _intervalInMinutes), _emailReciver);
             
            Logger.Info("Error sent.");
        }

        private async Task SendReport()
        {
            var actualHour = DateTime.Now.Hour;

            if( actualHour < _sendHour ) 
            {
                return;
            }

            var report = _reportRepository.GetLastNotSentReport();

            if( report == null ) 
            {
                return;
            }

            await _email.Send("Raport dobowy", _htmlEmail.GenerateReport(report), _emailReciver); 
            _reportRepository.ReportSent(report);
            Logger.Info("Report sent.");
        }        
    }
}

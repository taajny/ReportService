using ReportService.Models;
using ReportService.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private const int SendHour = 8;
        private const int IntervalInMinutes = 30;
        private Timer _timer = new Timer( IntervalInMinutes * 60000);
        private ErrorRepository _errorRepository = new ErrorRepository();
        private ReportRepository _reportRepository = new ReportRepository();
        private Email _email;
        private GenerateHtmlEmail _htmlEmail = new GenerateHtmlEmail();
        private string _emailReciver = "taajny@gmail.com";

        public ReportService()
        {
            InitializeComponent();

            _email = new Email(new EmailParams
            {
                HostSmtp = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                SenderName = "Krzysztof Stępczyński",
                SenderEmail = "testowanieaplikacji339@gmail.com",
                SenderEmailPassword = "czpk kxpg ojib ufok",
            });
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
                await SendReport();

            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private async Task SendError()
        {
            var errors = _errorRepository.GetErrors(IntervalInMinutes);        

            if( errors == null || !errors.Any() ) 
            {
                return;
            }

            await _email.Send("Błędy w aplikacji", _htmlEmail.GenerateErrors(errors, IntervalInMinutes), _emailReciver);
             
            Logger.Info("Error sent.");
        }

        private async Task SendReport()
        {
            var actualHour = DateTime.Now.Hour;

            if( actualHour < SendHour ) 
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

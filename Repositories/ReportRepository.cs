using ReportService.Models.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportService.Repositories
{
    public class ReportRepository
    {
        public  Report GetLastNotSentReport()
        {
            // pobieranie z bazy danych ostatniego raportu

            return new Report
            {
                Id = 1,
                Title = "R/1/2023",
                Date = new DateTime(2023, 1, 1, 12, 0, 0),
                Positions = new List<ReportPosition>
                { 
                    new ReportPosition
                    {
                        Id = 1,
                        ReportId = 1,
                        Title = "Possition 1",
                        Description = "Description 1",
                        Value = 43.01M
                    },

                    new ReportPosition
                    {
                        Id = 2,
                        ReportId = 1,
                        Title = "Possition 2",
                        Description = "Description 2",
                        Value = 43111M
                    },

                    new ReportPosition
                    {
                        Id = 3,
                        ReportId = 1,
                        Title = "Possition 3",
                        Description = "Description 3",
                        Value = 1.99M
                    }
                }
            };
        }
        public void ReportSent(Report report) 
        {
            report.IsSend = true;
            // zapis w bazie danych
        }
    }
}

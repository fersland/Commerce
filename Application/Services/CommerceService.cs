using Domain.Entities;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CommerceService
    {
        private readonly CommerceRepository _repository;

        public CommerceService()
        {
            _repository = new CommerceRepository();
        }

        public void ImportarCsv(Stream fileStream)
        {
            using (var reader = new StreamReader(fileStream))
            {
                bool esPrimeraLinea = true;
                string linea;

                while ((linea = reader.ReadLine()) != null)
                {
                    if (esPrimeraLinea)
                    {
                        esPrimeraLinea = false;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var values = linea.Split(',');

                    if (values.Length >= 3)
                    {
                        var commerce = new Commerce
                        {
                            PcProcessDate = values[0].Trim(),
                            PcNomComRed = values[1].Trim(),
                            PcNumDoc = values[2].Trim()
                        };

                        try
                        {
                            _repository.CreateCommerce(commerce);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al insertar: {ex.Message}");
                            throw new Exception($"Error en linea: {linea}. mensaje: {ex.Message}");
                        }
                    }
                }
            }
        }

        public int ValidateAndProcessDate(string processDate)
        {
            return _repository.ProcessCommerceValidation(processDate);
        }

        public List<CommerceQuarantine> GetQuarantineRecords()
        {
            return _repository.GetQuarantineList();
        }
    }
}

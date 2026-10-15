using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

using Application.Services;

namespace API.Controllers
{
    [RoutePrefix("api/commerce")]
    public class CommerceController : ApiController
    {
        private readonly CommerceService _commerceService;

        public CommerceController()
        {
            _commerceService = new CommerceService();
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IHttpActionResult> UploadCsv()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return BadRequest("El formato de la solicitud no es multipart/form-data.");
            }

            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                if (provider.Contents.Count == 0)
                {
                    return BadRequest("No se ha adjuntado ningun archivo.");
                }

                var fileContent = provider.Contents[0];
                var fileName = fileContent.Headers.ContentDisposition.FileName?.Trim('"');

                if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("El archivo debe tener extensión .csv.");
                }

                var inputStream = await fileContent.ReadAsStreamAsync();

                if (inputStream == null || inputStream.Length == 0)
                {
                    return BadRequest("El archivo CSV se encuentra vacio.");
                }

                _commerceService.ImportarCsv(inputStream);

                return Ok(new { message = "Archivo cargado correctamente." });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public class ProcessDateRequest
        {
            public string ProcessDate { get; set; }
        }

        [HttpPost]
        [Route("validarCuarentena")]
        public IHttpActionResult ProcessValidation([FromBody] ProcessDateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProcessDate))
            {
                return BadRequest("La fecha de proceso es obligatoria.");
            }

            try
            {
                int totalCuarentena = _commerceService.ValidateAndProcessDate(request.ProcessDate.Trim());

                return Ok(new
                {
                    processDate = request.ProcessDate,
                    totalQuarantineRecords = totalCuarentena,
                    message = $"Validacion completada. Se enviaron {totalCuarentena} registros."
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("listarCuarentena")]
        public IHttpActionResult GetQuarantine()
        {
            try
            {
                var quarantineList = _commerceService.GetQuarantineRecords();
                return Ok(quarantineList);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
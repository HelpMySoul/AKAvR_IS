using AKAvR_IS.Classes.FileInfo;
using AKAvR_IS.Interfaces.IFileService;
using Microsoft.AspNetCore.Mvc;

namespace AKAvR_IS.Controllers
{
    // Controllers/FilesController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileService fileService, ILogger<FilesController> logger)
        {
            _fileService = fileService;
            _logger      = logger;
        }

        /// <summary>
        /// Загрузка CSV файла
        /// </summary>
        /// <param name="file">CSV файл</param>
        /// <returns>Результат загрузки</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadCsvFile(IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new { message = "Файл не предоставлен" });
                }

                var result = await _fileService.UploadCsvFileAsync(file);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке файла");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Выгрузка файла по имени
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>Файл</returns>
        [HttpGet("download/{fileName}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                var (fileBytes, actualFileName, contentType) = await _fileService.DownloadFileAsync(fileName);

                return File(fileBytes, contentType, actualFileName);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Файл {fileName} не найден");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при выгрузке файла {fileName}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить список всех загруженных CSV файлов
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(IEnumerable<FileInfoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFilesList()
        {
            try
            {
                var files = await _fileService.GetUploadedFilesInfoAsync();
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка файлов");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        [HttpDelete("delete/{fileName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                var result = await _fileService.DeleteFileAsync(fileName);

                if (result)
                {
                    return Ok(new { message = "Файл успешно удален" });
                }

                return NotFound(new { message = "Файл не найден" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении файла {fileName}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}

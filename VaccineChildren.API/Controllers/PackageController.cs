using Microsoft.AspNetCore.Mvc;
using VaccineChildren.Application.DTOs.Request;
using VaccineChildren.Application.DTOs.Response;
using VaccineChildren.Application.Services;
using VaccineChildren.Core.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VaccineChildren.API.Controllers
{
    [Route("api/v1/package")]
    [ApiController]
    public class PackageController : BaseController
    {
        private readonly ILogger<PackageController> _logger;
        private readonly IPackageService _packageService;

        public PackageController(ILogger<PackageController> logger, IPackageService packageService)
        {
            _logger = logger;
            _packageService = packageService;
        }

        // POST api/v1/package
        [HttpPost]
        public async Task<IActionResult> CreatePackage([FromBody] PackageReq packageReq)
        {
            try
            {
                _logger.LogInformation("Creating new package");
                await _packageService.CreatePackage(packageReq);
                return Ok(BaseResponse<string>.OkResponse(null, "Package created successfully"));
            }
            catch (Exception e)
            {
                _logger.LogError("Error while creating package: {Error}", e.Message);
                return HandleException(e, "Internal Server Error");
            }
        }

        // GET api/v1/package/{packageId}
        [HttpGet("{packageId:guid}")]
        public async Task<IActionResult> GetPackageById(Guid packageId)
        {
            try
            {
                _logger.LogInformation("Fetching package by ID: {PackageId}", packageId);
                var package = await _packageService.GetPackageById(packageId);
                return Ok(BaseResponse<PackageRes>.OkResponse(package, "Package retrieved successfully"));
            }
            catch (KeyNotFoundException e)
            {
                _logger.LogError("Package not found: {Error}", e.Message);
                return NotFound(BaseResponse<string>.NotFoundResponse("Package not found"));
            }
            catch (Exception e)
            {
                _logger.LogError("Error while fetching package: {Error}", e.Message);
                return HandleException(e, "Internal Server Error");
            }
        }

        // PUT api/v1/package/{packageId}
        [HttpPut("{packageId:guid}")]
        public async Task<IActionResult> UpdatePackage(Guid packageId, [FromBody] PackageReq packageReq)
        {
            try
            {
                _logger.LogInformation("Updating package with ID: {PackageId}", packageId);
                await _packageService.UpdatePackage(packageId, packageReq);
                return Ok(BaseResponse<string>.OkResponse(null, "Package updated successfully"));
            }
            catch (KeyNotFoundException e)
            {
                _logger.LogError("Package not found: {Error}", e.Message);
                return NotFound(BaseResponse<string>.NotFoundResponse("Package not found"));
            }
            catch (Exception e)
            {
                _logger.LogError("Error while updating package: {Error}", e.Message);
                return HandleException(e, "Internal Server Error");
            }
        }

        // DELETE api/v1/package/{packageId}
        [HttpDelete("{packageId:guid}")]
        public async Task<IActionResult> DeletePackage(Guid packageId)
        {
            try
            {
                _logger.LogInformation("Deleting package with ID: {PackageId}", packageId);
                await _packageService.DeletePackage(packageId);
                return Ok(BaseResponse<string>.OkResponse(null, "Package deleted successfully"));
            }
            catch (KeyNotFoundException e)
            {
                _logger.LogError("Package not found: {Error}", e.Message);
                return NotFound(BaseResponse<string>.NotFoundResponse("Package not found"));
            }
            catch (Exception e)
            {
                _logger.LogError("Error while deleting package: {Error}", e.Message);
                return HandleException(e, "Internal Server Error");
            }
        }

        // GET api/v1/package
        [HttpGet]
        public async Task<IActionResult> GetAllPackages()
        {
            try
            {
                _logger.LogInformation("Fetching all packages");
                var packageList = await _packageService.GetAllPackages();
                return Ok(BaseResponse<List<PackageRes>>.OkResponse(packageList, "Packages retrieved successfully"));
            }
            catch (Exception e)
            {
                _logger.LogError("Error while fetching all packages: {Error}", e.Message);
                return HandleException(e, "Internal Server Error");
            }
        }
    }
}

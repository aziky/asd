using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using VaccineChildren.Application.DTOs.Request;
using VaccineChildren.Application.DTOs.Response;
using VaccineChildren.Core.Store;
using VaccineChildren.Domain.Abstraction;
using VaccineChildren.Domain.Entities;

namespace VaccineChildren.Application.Services.Impl;

public class UserService : IUserService
{
    private const string DateFormat = "dd-MM-yyyy";
    
    private readonly ILogger<IUserService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRsaService _rsaService;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(ILogger<IUserService> logger, IUnitOfWork unitOfWork, IMapper mapper, 
        IConfiguration configuration, IRsaService rsaService, ICacheService cacheService,
        IEmailService emailService, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _rsaService = rsaService;
        _cacheService = cacheService;
        _emailService = emailService;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }
    // public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest registerRequest)
    // {
    //     try
    //     {
    //         _logger.LogInformation("Start registering user");
    //         _unitOfWork.BeginTransaction();
    //
    //         var userRepository = _unitOfWork.GetRepository<User>();
    //
    //         // Check if email already exists
    //         var existingUser = await userRepository.FindByConditionAsync(u => u.Email == registerRequest.Email);
    //
    //         if (existingUser != null)
    //         {
    //             _logger.LogWarning("Email already exists: {Email}", registerRequest.Email);
    //             return new RegisterResponse
    //             {
    //                 Success = false,
    //                 Message = "Email already exists"
    //             };
    //         }
    //
    //         var hashedPassword = _rsaService.Encrypt(registerRequest.Password);
    //         registerRequest.Password = hashedPassword;
    //
    //         // Map request to User entity
    //         var userEntity = _mapper.Map<User>(registerRequest);
    //         userEntity.CreatedBy = registerRequest.UserName;
    //         userEntity.CreatedAt = DateTime.UtcNow.ToLocalTime();
    //
    //         // Assign the 'user' role to the new user
    //         var roleRepository = _unitOfWork.GetRepository<Role>();
    //         var userRole = await roleRepository.FindByConditionAsync(
    //             r => r.RoleName.ToLower() == StaticEnum.RoleEnum.User.ToString().ToLower());
    //         
    //         if (userRole == null)
    //         {
    //             _logger.LogError("User role not found in database");
    //             return new RegisterResponse
    //             {
    //                 Success = false,
    //                 Message = "Error assigning user role"
    //             };
    //         }
    //
    //         userEntity.RoleId = userRole.RoleId;
    //
    //         await userRepository.InsertAsync(userEntity);
    //         await _unitOfWork.SaveChangeAsync();
    //
    //         _unitOfWork.CommitTransaction();
    //         _logger.LogInformation("User registration successful");
    //
    //         return new RegisterResponse
    //         {
    //             Success = true,
    //             Message = "User registered successfully"
    //         };
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError("Error at register user: {Message}", e.Message);
    //         _unitOfWork.RollBack();
    //         throw new Exception("An error occurred while registering the user", e);
    //     }
    //     finally
    //     {
    //         _unitOfWork.Dispose();
    //     }
    // }
    public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest registerRequest)
{
    try
    {
        _logger.LogInformation("Start registering user");
        _unitOfWork.BeginTransaction();

        var userRepository = _unitOfWork.GetRepository<User>();

        // Check if email already exists
        var existingUser = await userRepository.FindByConditionAsync(u => u.Email == registerRequest.Email);

        if (existingUser != null)
        {
            _logger.LogWarning("Email already exists: {Email}", registerRequest.Email);
            return new RegisterResponse
            {
                Success = false,
                Message = "Email already exists"
            };
        }

        var hashedPassword = _rsaService.Encrypt(registerRequest.Password);
        registerRequest.Password = hashedPassword;

        // Map request to User entity
        var userEntity = _mapper.Map<User>(registerRequest);
        userEntity.CreatedBy = registerRequest.UserName;
        userEntity.CreatedAt = DateTime.UtcNow.ToLocalTime();
        
        // Set email verification status to false and generate verification token
        userEntity.IsVerified = false;
        userEntity.EmailVerificationToken = Guid.NewGuid().ToString();
        userEntity.TokenExpiry = DateTime.UtcNow.AddDays(1); // Token valid for 1 days

        // Assign the 'user' role to the new user
        var roleRepository = _unitOfWork.GetRepository<Role>();
        var userRole = await roleRepository.FindByConditionAsync(
            r => r.RoleName.ToLower() == StaticEnum.RoleEnum.User.ToString().ToLower());
        
        if (userRole == null)
        {
            _logger.LogError("User role not found in database");
            return new RegisterResponse
            {
                Success = false,
                Message = "Error assigning user role"
            };
        }

        userEntity.RoleId = userRole.RoleId;

        await userRepository.InsertAsync(userEntity);
        await _unitOfWork.SaveChangeAsync();

        // Send verification email
        await SendVerificationEmail(userEntity);

        _unitOfWork.CommitTransaction();
        _logger.LogInformation("User registration successful, verification email sent");

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful. Please check your email to verify your account."
        };
    }
    catch (Exception e)
    {
        _logger.LogError("Error at register user: {Message}", e.Message);
        _unitOfWork.RollBack();
        throw new Exception("An error occurred while registering the user", e);
    }
    finally
    {
        _unitOfWork.Dispose();
    }
}

    private async Task SendVerificationEmail(User user)
    {
        // Get template ID from enum
        int templateId = StaticEnum.EmailTemplateEnum.EmailVerification.Id();
    
        // Create verification link with token
        string verificationUrl = $"{_configuration["AppUrl"]}/verify-email?token={user.EmailVerificationToken}&email={Uri.EscapeDataString(user.Email)}";
    
        var templateData = new Dictionary<string, string>
        {
            { "UserName", user.UserName },
            { "VerificationLink", verificationUrl },
            { "VerificationButton", $"<a href='{verificationUrl}' style='display:inline-block;background-color:#c10c0c;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;font-weight:bold;'>CONFIRM YOUR EMAIL</a>" }
        };
    
        await _emailService.SendEmailAsync(
            user.Email,
            user.UserName,
            templateId,
            templateData
        );
    }
    
    public async Task<UserRes> Login(UserReq userReq)
    {
        try
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            var user = await userRepository.FindByConditionAsync(u => u.Email == userReq.Email);
            
            if (user == null || !VerifyPassword(userReq.Password, user.Password))
            {
                _logger.LogInformation("Login failed");
                throw new KeyNotFoundException("Invalid username or password");
            }

            // Chỉ get cache sau khi đã xác thực password thành công
            string cacheKey = $"user_{userReq.Email}";
            var cachedUser = await _cacheService.GetAsync<UserRes>(cacheKey);
            if (cachedUser != null)
            {
                return cachedUser;
            }

            var token = GenerateJwtToken(user);
            var response = _mapper.Map<UserRes>(user);
            response.Token = token;
            response.RoleName = user.Role?.RoleName ?? "Unknown";

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(1));

            return response;
        }
        catch (Exception e)
        {
            _logger.LogError("Error at login: {Message}", e.Message);
            throw;
        }
        finally
        {
            _unitOfWork.Dispose();
        }
    }

    

    private bool VerifyPassword(string inputPassword, string encryptedPassword)
    {
        string decryptedPassword = _rsaService.Decrypt(encryptedPassword);
        return decryptedPassword != null && inputPassword == decryptedPassword;
    }

    private string GenerateJwtToken(User user)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var signingCredentials = _rsaService.GetSigningCredentials();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.UserId.ToString()),
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = signingCredentials,
                Issuer = "VaccineChildren",
                Audience = "VaccineChildren.API"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error generating JWT token: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to generate JWT token.", ex);
        }
    }
    
     public async Task CreateChildAsync(CreateChildReq request)
    {
        try
        {
            _logger.LogInformation("Start creating child profile");
            _unitOfWork.BeginTransaction();
            var childRepository = _unitOfWork.GetRepository<Child>();
            var userRepository = _unitOfWork.GetRepository<User>();
            
            
            var user = await userRepository.GetByIdAsync(Guid.Parse(request.UserId));
            if (user == null)
            {
                _logger.LogError("User not found");
                throw new KeyNotFoundException("User not found");
            }

            _logger.LogInformation("Start creating child");
            var child = _mapper.Map<Child>(request);
            child.Dob = DateOnly.ParseExact(request.Dob, DateFormat, CultureInfo.InvariantCulture);
            child.CreatedAt = DateTime.Now;
            child.CreatedBy = user.Email;
            await childRepository.InsertAsync(child);
            await _unitOfWork.SaveChangeAsync();
            _unitOfWork.CommitTransaction();
            
            _logger.LogInformation("Child created successfully");
        }
        catch (Exception e)
        {
            _unitOfWork.RollBack();
            _logger.LogError("Error at create child async cause by: {Message}", e.Message);
            throw;
        }
        finally
        {
            _unitOfWork.Dispose();
        }
    }

    public async Task<GetChildRes> GetChildByChildIdAsync(string childId)
    {
        try
        {
            _logger.LogInformation("Start getting child profile");
            
            var childRepository = _unitOfWork.GetRepository<Child>();
            var child = await childRepository.GetByIdNoTracking(Guid.Parse(childId));
            if (child == null) throw new KeyNotFoundException("Child not found");
            
            return _mapper.Map<GetChildRes>(child);
        }
        catch (Exception e)
        {
            _logger.LogError("Error at get child async cause by: {}", e.Message);
            throw;
        }
    }

    public async Task<GetUserRes> GetUserByUserIdAsync()
    {
        try
        {
            string id = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Name).Value;
            _logger.LogInformation("Start get user profile with user id {}", id);
            var userRepository = _unitOfWork.GetRepository<User>();

            var user = await userRepository.GetAllAsync(query => query.Include(u => u.Children)
                    .Where(u => u.UserId.ToString().Equals(id)));

            
            return _mapper.Map<GetUserRes>(user.FirstOrDefault());
            
        }
        catch (Exception e)
        {
            _logger.LogError("Error at get user by user id async cause by: {}", e.Message);
            throw;
        }
    }
}

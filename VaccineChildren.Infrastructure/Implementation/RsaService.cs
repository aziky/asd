﻿using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace VaccineChildren.Application.Services.Impl;

public class RsaService : IRsaService, IDisposable
{
    private readonly RSA _rsa;
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;
    private const int KEY_SIZE = 2048;
    
    public RsaService(string privateKeyPath = "private_key.rsa", string publicKeyPath = "public_key.rsa")
    {
        _privateKeyPath = privateKeyPath;
        _publicKeyPath = publicKeyPath;
        _rsa = RSA.Create(KEY_SIZE);
        InitializeRsaKeys();
    }

    private void InitializeRsaKeys()
    {
        if (File.Exists(_privateKeyPath))
        {
            try
            {
                // Load existing private key in PKCS8 format
                byte[] privateKeyBytes = File.ReadAllBytes(_privateKeyPath);
                _rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                
                // Generate public key if it doesn't exist
                if (!File.Exists(_publicKeyPath))
                {
                    ExportPublicKey();
                }
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to load RSA key. The key file may be corrupted.", ex);
            }
        }
        else
        {
            // Generate and save new keys
            GenerateAndSaveNewKeys();
        }
    }

    private void GenerateAndSaveNewKeys()
    {
        try
        {
            // Export and save private key
            byte[] privateKeyBytes = _rsa.ExportPkcs8PrivateKey();
            File.WriteAllBytes(_privateKeyPath, privateKeyBytes);

            // Export and save public key
            ExportPublicKey();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate and save RSA keys.", ex);
        }
    }

    private void ExportPublicKey()
    {
        byte[] publicKeyBytes = _rsa.ExportSubjectPublicKeyInfo();
        File.WriteAllBytes(_publicKeyPath, publicKeyBytes);
    }

    public string Encrypt(string data)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentNullException(nameof(data));

        try
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] encryptedBytes = _rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Encryption failed. The data may be too large for RSA encryption.", ex);
        }
    }

    public string Decrypt(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            throw new ArgumentNullException(nameof(encryptedData));

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
            byte[] decryptedBytes = _rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Invalid Base64 string.", nameof(encryptedData), ex);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Decryption failed. The data may be corrupted or was encrypted with a different key.", ex);
        }
    }

    public (string PrivateKeyPath, string PublicKeyPath) GetKeyPaths()
    {
        return (_privateKeyPath, _publicKeyPath);
    }

    public void Dispose()
    {
        _rsa?.Dispose();
    }
    public SecurityKey GetRsaSecurityKey()
    {
        try
        {
            // Create RSA security key from the private key
            return new RsaSecurityKey(_rsa);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create RSA security key.", ex);
        }
    }

    public SigningCredentials GetSigningCredentials()
    {
        try
        {
            var securityKey = GetRsaSecurityKey();
            return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create signing credentials.", ex);
        }
    }
}
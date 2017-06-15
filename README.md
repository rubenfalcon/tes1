# 

https://maps-apis.googleblog.com/2015/06/code-road-android-app-store-your.html

https://www.codeproject.com/Articles/507651/Customized-Android-ListView-with-Image-and-Text

https://inducesmile.com/android/android-6-marshmallow-runtime-permissions-request-example/

https://www.captechconsulting.com/blogs/runtime-permissions-best-practices-and-how-to-gracefully-handle-permission-removal

https://github.com/googlesamples/android-RuntimePermissions/blob/master/Application/src/main/java/com/example/android/system/runtimepermissions/MainActivity.java

https://blogs.technet.microsoft.com/kv/2016/09/26/get-started-with-azure-key-vault-certificates/

http://www.rahulpnath.com/blog/pfx-certificate-in-azure-key-vault/

https://blogs.technet.microsoft.com/kv/2016/09/26/get-started-with-azure-key-vault-certificates/

http://itext.2136553.n4.nabble.com/Luna-SA-HSM-Integration-with-iTextSharp-td2552278.html

For the sake of closure and perhaps saving someone else from running off on the same wild goose chase in the future, I thought I would post an update here. 

The issue was related to my own lack of understanding of the Luna SA HSM.  The setup instructions with the device included generation of a client certificate that was then installed on the HSM.  I took this to mean that the client certificate that was generated as part of that process was in fact the certificate I would be using for signing. 

After speaking with SafeNet support, I discovered that the client certificate referred to in the documentation was not for general use and was related to establishing the secure trusted relationship between the HSM and my computer. 

Once I realized this, I was able to generate a test self-signed certificate using makecert and just specifying the correct parameters: 

        makecert -sk keyContainerName -sp "Luna Cryptographic Services for Microsoft Windows" -sy 1, -r -n "CN=issuer" -ss my test.cer 

Once this was done, I had a certificate in the "My" store that essentially contained pointers to the keys stored on the HSM.  I was then able to use the example code to quickly have success in signing the PDF. 

Another case of an early misunderstanding leading to over-thinking and over-engineering (that in this case still didn't end up working).  In reality, this integration is pretty straight forward.  I'm including the sample C# code that I have working for signing a PDF using the Luna HSM below. Thanks to all of you that responded. 

Mike 


        static void SignPdf() 
        { 
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser); 
            store.Open(OpenFlags.MaxAllowed); 

            X509Certificate2 cert = null; 
            int i = 0; 
            while ((i < store.Certificates.Count) && (cert == null)) 
            { 
                if (store.Certificates[i].Subject == "CN=name") 
                    cert = store.Certificates[i]; 
                else 
                    i++; 
            } 

            Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser(); 
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[]{ cp.ReadCertificate(cert.RawData) }; 

            PdfReader reader = new PdfReader(@"C:\file.pdf"); 
            PdfStamper stp = PdfStamper.CreateSignature(reader, new FileStream("c:\\file_signed.pdf", FileMode.Create), '\0'); 
            stp.FormFlattening = true; 

            PdfSignatureAppearance sap = stp.SignatureAppearance; 
            sap.SignDate = DateTime.Now; 
            sap.SetCrypto(null, chain, null, null); 
            sap.Reason = "PDF Signing"; 
            sap.Location = "My Location"; 
            sap.Acro6Layers = true; 
            sap.Render = PdfSignatureAppearance.SignatureRender.NameAndDescription; 

            PdfSignature dic = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1); 
            dic.Date = new PdfDate(sap.SignDate); 
            dic.Name = PdfPKCS7.GetSubjectFields(chain[0]).GetField("CN"); 
            if (sap.Reason != null) 
                dic.Reason = sap.Reason; 
            if (sap.Location != null) 
                dic.Location = sap.Location; 
            sap.CryptoDictionary = dic; 

            int csize = 4000; 
            Hashtable exc = new Hashtable(); 
            exc[PdfName.CONTENTS] = csize * 2 + 2; 
            sap.PreClose(exc); 

            HashAlgorithm sha = new SHA1CryptoServiceProvider(); 

            Stream s = sap.RangeStream; 
            int read = 0; 
            byte[] buff = new byte[8192]; 
            while ((read = s.Read(buff, 0, 8192)) > 0) 
                sha.TransformBlock(buff, 0, read, buff, 0); 
            sha.TransformFinalBlock(buff, 0, 0); 

            byte[] pk = SignMsg(sha.Hash, cert, false); 
            byte[] outc = new byte[csize]; 
            Array.Copy(pk, 0, outc, 0, pk.Length); 

            PdfDictionary dic2 = new PdfDictionary(); 
            dic2.Put(PdfName.CONTENTS, new PdfString(outc).SetHexWriting(true)); 
            sap.Close(dic2); 
        } 
        static public byte[] SignMsg(Byte[] msg, X509Certificate2 signerCert, bool detached) 
        { 
            //  Place message in a ContentInfo object. 
            //  This is required to build a SignedCms object. 
            ContentInfo contentInfo = new ContentInfo(msg); 

            //  Instantiate SignedCms object with the ContentInfo above. 
            //  Has default SubjectIdentifierType IssuerAndSerialNumber. 
            SignedCms signedCms = new SignedCms(contentInfo, detached); 

            //  Formulate a CmsSigner object for the signer. 
            CmsSigner cmsSigner = new CmsSigner(signerCert); 

            // Include the following line if the top certificate in the 
            // smartcard is not in the trusted list. 
            cmsSigner.IncludeOption = X509IncludeOption.EndCertOnly; 

            //  Sign the CMS/PKCS #7 message. The second argument is 
            //  needed to ask for the pin. 
            signedCms.ComputeSignature(cmsSigner, false); 

            //  Encode the CMS/PKCS #7 message. 
            return signedCms.Encode(); 
        } 

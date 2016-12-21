// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License in the project root for license information.

'use strict';
var assert = require("assert"),
  fs = require('fs'),
  glob = require('glob'),
  path = require('path'),
  _ = require('lodash'),
  z = require('z-schema'),
  request = require('request'),
  util = require('util');

var extensionSwaggerSchemaUrl = "https://raw.githubusercontent.com/Azure/autorest/master/schema/swagger-extensions.json";
var swaggerSchemaUrl = "http://json.schemastore.org/swagger-2.0";
var swaggerSchemaAltUrl = "http://swagger.io/v2/schema.json";
var schemaUrl = "http://json-schema.org/draft-04/schema";
var exampleSchemaUrl = "https://raw.githubusercontent.com/Azure/autorest/master/schema/example-schema.json";
var compositeSchemaUrl = "https://raw.githubusercontent.com/Azure/autorest/master/schema/composite-swagger.json";

var swaggerSchema, extensionSwaggerSchema, schema4, exampleSchema, compositeSchema, 
    globPath, compositeGlobPath, swaggers, compositeSwaggers, validator;

globPath = path.join(__dirname, '../', '/**/swagger/*.json');
swaggers = _(glob.sync(globPath));

// Useful when debugging a test for a particular swagger. 
// Just update the regex. That will return an array of filtered items.
// swaggers = swaggers.filter(function(item) {
//   return (item.match(/.*arm-redis.*/ig) !== null);
// })

compositeGlobPath = path.join(__dirname, '../', '/**/composite*.json');
compositeSwaggers = _(glob.sync(compositeGlobPath));

// Remove byte order marker. This catches EF BB BF (the UTF-8 BOM)
// because the buffer-to-string conversion in `fs.readFile()`
// translates it to FEFF, the UTF-16 BOM.
function stripBOM(content) {
  if (Buffer.isBuffer(content)) {
    content = content.toString();
  }
  if (content.charCodeAt(0) === 0xFEFF || content.charCodeAt(0) === 0xFFFE) {
    content = content.slice(1);
  }
  return content;
}

describe('Azure Swagger Schema Validation', function() {
  before(function(done) {
    request({url: extensionSwaggerSchemaUrl, json:true}, function (error, response, extensionSwaggerSchemaBody) {        
      request({ url: swaggerSchemaAltUrl, json: true }, function (error, response, swaggerSchemaBody) {
        request({ url: exampleSchemaUrl, json: true }, function (error, response, exampleSchemaBody) {
          request({ url: compositeSchemaUrl, json: true }, function (error, response, compositeSchemaBody) {
            extensionSwaggerSchema = extensionSwaggerSchemaBody;
            swaggerSchema = swaggerSchemaBody;
            exampleSchema = exampleSchemaBody;
            compositeSchema = compositeSchemaBody;
            validator = new z({ breakOnFirstError: false });
            validator.setRemoteReference(swaggerSchemaUrl, swaggerSchema);
            validator.setRemoteReference(exampleSchemaUrl, exampleSchema);
            validator.setRemoteReference(compositeSchemaUrl, compositeSchema);
            done();
          });
        });
      });
    });
  });

  _(swaggers).each(function(swagger){
    it(swagger + ' should be a valid Swagger document.', function(done){
      fs.readFile(swagger, 'utf8', function(err, data) {
        if(err) { done(err); }
        var parsedData;
        try {
          parsedData = JSON.parse(stripBOM(data));
        } catch (err) {
          throw new Error("swagger " + swagger + " is an invalid JSON. " + util.inspect(err, {depth: null}));
        }
        
        if (parsedData.documents && util.isArray(parsedData.documents)) {
          console.log(util.format('Skipping the test for \'%s\' document as it seems to be a composite swagger doc.', swagger));
          done();
        }
        var valid = validator.validate(parsedData, extensionSwaggerSchema);
        if (!valid) {
            var error = validator.getLastErrors();
            throw new Error("Schema validation failed: " + util.inspect(error, {depth: null}));
        }
        assert(valid === true);
        done();
      });
    });
  }).value();

  _(compositeSwaggers).each(function(compositeSwagger){
    it('composite: ' + compositeSwagger + ' should be a valid Composite Swagger document.', function(done){
      fs.readFile(compositeSwagger, 'utf8', function(err, data) {
        if(err) { done(err); }
        var parsedData;
        try {
          parsedData = JSON.parse(stripBOM(data));
        } catch (err) {
          throw new Error("compositeSwagger " + compositeSwagger + " is an invalid JSON. " + util.inspect(err, {depth: null}));
        }
        var valid = validator.validate(parsedData, compositeSchema);
        if (!valid) {
            var error = validator.getLastErrors();
            throw new Error("Schema validation for Composite Swagger failed: " + util.inspect(error, {depth: null}));
        }
        assert(valid === true);
        done();
      });
    });
  }).value();
});

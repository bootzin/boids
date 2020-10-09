﻿#version 330 core
out vec4 FragColor;

in vec2 TexCoords;
in vec3 Normal;  
in vec3 FragPos;
in vec4 FragPosLightSpace;
in mat3 TBN;

uniform vec4 lightPos; 
uniform vec3 lightColor;
uniform vec2 attenuation; // linear and quadratic (constant is always 1)
uniform vec2 cutOff; //inner, outer

uniform vec3 viewPos; 

uniform sampler2D texture_diffuse1;
uniform sampler2D texture_specular1;
uniform sampler2D texture_normal1;
uniform sampler2D shadowMap;

float ShadowCalculation(vec4 fragPosLightSpace)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(shadowMap, projCoords.xy).r;
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    // calculate bias (based on depth map resolution and slope)
    vec3 normal = normalize(Normal);
    vec3 lightDir = normalize(lightPos.xyz - FragPos);
    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);
    // check whether current frag pos is in shadow
    // float shadow = currentDepth - bias > closestDepth  ? 1.0 : 0.0;
    // PCF
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
            shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;        
        }    
    }
    shadow /= 9.0;
    
    // keep the shadow at 0.0 when outside the far_plane region of the light's frustum.
    if(projCoords.z > 1.0)
        shadow = 0.0;
        
    return shadow;
}

void main()
{
    vec4 texDiffColor = texture(texture_diffuse1, TexCoords);
    if(texDiffColor.a < .9)
        discard;

    // ambient
    float ambientStrength = 0.2;
    vec3 ambient = ambientStrength * lightColor * texDiffColor.rgb;
  	
    // diffuse
    float diffuseStrength = 1;
    vec3 norm = texture(texture_normal1, TexCoords).rgb;
    norm = norm * 2.0 - 1.0;   
    norm = normalize(TBN * norm);

    vec3 lightDir;
    if (lightPos.w == 0.0)
        lightDir = normalize((-lightPos).xyz);
    else if (lightPos.w == 1.0)
        lightDir = normalize(lightPos.xyz - FragPos);

    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor * texDiffColor.rgb * diffuseStrength;
    
    // specular
    float specularStrength = 0.4;
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, norm);  
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 16);
    vec3 specular = specularStrength * spec * lightColor * (texture(texture_specular1, TexCoords)).rgb;

    // spotlight (soft edges)
    //float theta = dot(lightDir, normalize(-lightPos.xyz)); 
    //float epsilon = (cutOff.x - cutOff.y);
    //float intensity = clamp((theta - cutOff.y) / epsilon, 0.0, 1.0);
    //diffuse  *= intensity;
    //specular *= intensity;

    // attenuation
    float distance = length(lightPos.xyz - FragPos);
    float atten = 1.0 / (1.0 + attenuation.x * distance + attenuation.y * (distance * distance));

    float shadow = ShadowCalculation(FragPosLightSpace); 
    vec3 result = (ambient + (1.0 - shadow) * (diffuse + specular)) * atten;
    FragColor = vec4(result, 1);
}
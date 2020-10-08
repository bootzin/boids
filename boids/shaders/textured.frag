#version 330 core
out vec4 FragColor;

in vec2 TexCoords;
in vec3 Normal;  
in vec3 FragPos; 

uniform vec4 lightPos; 
uniform vec3 lightColor;
uniform vec2 attenuation; // linear and quadratic (constant is always 1)
uniform vec2 cutOff; //inner, outer

uniform vec3 viewPos; 

uniform sampler2D texture_diffuse1;
uniform sampler2D texture_specular1;

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
    vec3 norm = normalize(Normal);

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
    float distance    = length(lightPos.xyz - FragPos);
    float atten = 1.0 / (1.0 + attenuation.x * distance + attenuation.y * (distance * distance));

    vec3 result = (ambient + diffuse + specular) * atten;
    FragColor = vec4(result, 1);
}
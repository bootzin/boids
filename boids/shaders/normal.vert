#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in vec3 aTangent;
layout (location = 4) in vec3 aBitangent;

out vec2 TexCoords;
out vec3 FragPos;

// shadows
out vec4 FragPosLightSpace;
out vec4 TangentFragPosLightSpace;

//normals
out vec4 TangentLightPos;
out vec3 TangentViewPos;
out vec3 TangentFragPos;

uniform vec3 viewPos; 
uniform vec4 lightPos; 
uniform mat4 lightSpaceMatrix;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    FragPos = vec3(model * vec4(aPos, 1.0));
    TexCoords = aTexCoords;

    mat3 normalMatrix = transpose(inverse(mat3(model)));
    vec3 T = normalize(normalMatrix * aTangent);
    vec3 N = normalize(normalMatrix * aNormal);
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);

    mat3 TBN = transpose(mat3(T, B, N));    
    TangentLightPos = vec4(TBN * lightPos.xyz, lightPos.w);
    TangentViewPos  = TBN * viewPos;
    TangentFragPos  = TBN * FragPos;

    FragPosLightSpace = lightSpaceMatrix * vec4(FragPos, 1.0);
    TangentFragPosLightSpace = mat4(TBN) * FragPosLightSpace;

    gl_Position = projection * view * model * vec4(aPos, 1.0);
}
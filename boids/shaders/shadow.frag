#version 330 core

in vec2 TexCoords;

uniform sampler2D texture_diffuse1;

void main()
{
    vec4 texDiffColor = texture(texture_diffuse1, TexCoords);
    if(texDiffColor.a < .9)
        discard;
    // gl_FragDepth = gl_FragCoord.z;
} 
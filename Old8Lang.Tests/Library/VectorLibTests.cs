using Old8LangLib;

namespace Old8Lang.Tests.Library;

/// <summary>
/// 向量库测试，验证各种维度向量的功能
/// </summary>
public class VectorLibTests
{
    // Vector2 测试
    [Fact]
    public void Vector2_ConstructorAndProperties_WorkCorrectly()
    {
        // Arrange & Act
        Vector2 v = new Vector2(3, 4);

        // Assert
        Assert.Equal(3, v.X);
        Assert.Equal(4, v.Y);
        Assert.Equal(5, v.Magnitude, 1e-10);
        Assert.Equal(25, v.SqrMagnitude);
    }

    [Fact]
    public void Vector2_Normalization_WorksCorrectly()
    {
        // Arrange
        Vector2 v = new Vector2(3, 4);

        // Act
        Vector2 normalized = v.Normalized;

        // Assert
        Assert.Equal(1, normalized.Magnitude, 1e-10);
        Assert.Equal(0.6, normalized.X, 1e-10);
        Assert.Equal(0.8, normalized.Y, 1e-10);

        // Test in-place normalization
        Vector2 vCopy = new Vector2(3, 4);
        vCopy.Normalize();
        Assert.Equal(1, vCopy.Magnitude, 1e-10);
    }

    [Fact]
    public void Vector2_ArithmeticOperations_WorkCorrectly()
    {
        // Arrange
        Vector2 v1 = new Vector2(3, 4);
        Vector2 v2 = new Vector2(5, 6);

        // Act & Assert
        Vector2 sum = v1 + v2;
        Assert.Equal(8, sum.X);
        Assert.Equal(10, sum.Y);

        Vector2 difference = v1 - v2;
        Assert.Equal(-2, difference.X);
        Assert.Equal(-2, difference.Y);

        Vector2 product1 = v1 * 2;
        Assert.Equal(6, product1.X);
        Assert.Equal(8, product1.Y);

        Vector2 product2 = 2 * v1;
        Assert.Equal(6, product2.X);
        Assert.Equal(8, product2.Y);

        Vector2 quotient = v1 / 2;
        Assert.Equal(1.5, quotient.X);
        Assert.Equal(2, quotient.Y);

        Vector2 negation = -v1;
        Assert.Equal(-3, negation.X);
        Assert.Equal(-4, negation.Y);
    }

    [Fact]
    public void Vector2_DotAndCrossProduct_WorkCorrectly()
    {
        // Arrange
        Vector2 v1 = new Vector2(3, 4);
        Vector2 v2 = new Vector2(5, 6);

        // Act & Assert
        double dot = v1.Dot(v2);
        Assert.Equal(3 * 5 + 4 * 6, dot);

        double cross = v1.Cross(v2);
        Assert.Equal(3 * 6 - 4 * 5, cross);
    }

    [Fact]
    public void Vector2_AngleAndDistance_WorkCorrectly()
    {
        // Arrange
        Vector2 v1 = new Vector2(1, 0);
        Vector2 v2 = new Vector2(0, 1);

        // Act & Assert
        double angle = v1.Angle(v2);
        Assert.Equal(Math.PI / 2, angle, 1e-10);

        double distance = v1.Distance(v2);
        Assert.Equal(Math.Sqrt(2), distance, 1e-10);
    }

    [Fact]
    public void Vector2_LerpAndProject_WorkCorrectly()
    {
        // Arrange
        Vector2 v1 = new Vector2(0, 0);
        Vector2 v2 = new Vector2(10, 10);
        Vector2 v3 = new Vector2(3, 4);

        // Act & Assert
        Vector2 lerp = v1.Lerp(v2, 0.5);
        Assert.Equal(5, lerp.X);
        Assert.Equal(5, lerp.Y);

        Vector2 project = v3.Project(Vector2.UnitX);
        Assert.Equal(3, project.X);
        Assert.Equal(0, project.Y);
    }

    [Fact]
    public void Vector2_Reflect_WorksCorrectly()
    {
        // Arrange
        Vector2 v = new Vector2(1, -1); // 从右下方向撞击
        Vector2 normal = Vector2.UnitY; // 垂直向上的法线

        // Act
        Vector2 reflected = v.Reflect(normal);

        // Assert
        Assert.Equal(1, reflected.X, 1e-10);
        Assert.Equal(1, reflected.Y, 1e-10);
    }

    [Fact]
    public void Vector2_Equality_WorksCorrectly()
    {
        // Arrange
        Vector2 v1 = new Vector2(3, 4);
        Vector2 v2 = new Vector2(3, 4);
        Vector2 v3 = new Vector2(5, 6);

        // Act & Assert
        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.True(v1 != v3);
        Assert.False(v1 != v2);
        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void Vector2_StaticVectors_WorkCorrectly()
    {
        // Act & Assert
        Assert.Equal(0, Vector2.Zero.X);
        Assert.Equal(0, Vector2.Zero.Y);

        Assert.Equal(1, Vector2.UnitX.X);
        Assert.Equal(0, Vector2.UnitX.Y);

        Assert.Equal(0, Vector2.UnitY.X);
        Assert.Equal(1, Vector2.UnitY.Y);

        Assert.Equal(1, Vector2.One.X);
        Assert.Equal(1, Vector2.One.Y);
    }

    // Vector3 测试
    [Fact]
    public void Vector3_ConstructorAndProperties_WorkCorrectly()
    {
        // Arrange & Act
        Vector3 v = new Vector3(1, 2, 3);

        // Assert
        Assert.Equal(1, v.X);
        Assert.Equal(2, v.Y);
        Assert.Equal(3, v.Z);
        Assert.Equal(Math.Sqrt(14), v.Magnitude, 1e-10);
        Assert.Equal(14, v.SqrMagnitude);
    }

    [Fact]
    public void Vector3_CrossProduct_WorksCorrectly()
    {
        // Arrange
        Vector3 v1 = new Vector3(1, 0, 0);
        Vector3 v2 = new Vector3(0, 1, 0);

        // Act
        Vector3 cross = v1.Cross(v2);

        // Assert
        Assert.Equal(Vector3.UnitZ, cross);
    }

    [Fact]
    public void Vector3_ArithmeticOperations_WorkCorrectly()
    {
        // Arrange
        Vector3 v1 = new Vector3(1, 2, 3);
        Vector3 v2 = new Vector3(4, 5, 6);

        // Act & Assert
        Vector3 sum = v1 + v2;
        Assert.Equal(5, sum.X);
        Assert.Equal(7, sum.Y);
        Assert.Equal(9, sum.Z);

        Vector3 difference = v1 - v2;
        Assert.Equal(-3, difference.X);
        Assert.Equal(-3, difference.Y);
        Assert.Equal(-3, difference.Z);

        Vector3 product = v1 * 2;
        Assert.Equal(2, product.X);
        Assert.Equal(4, product.Y);
        Assert.Equal(6, product.Z);
    }

    [Fact]
    public void Vector3_StaticVectors_WorkCorrectly()
    {
        // Act & Assert
        Assert.Equal(Vector3.Zero, new Vector3(0, 0, 0));
        Assert.Equal(Vector3.UnitX, new Vector3(1, 0, 0));
        Assert.Equal(Vector3.UnitY, new Vector3(0, 1, 0));
        Assert.Equal(Vector3.UnitZ, new Vector3(0, 0, 1));
        Assert.Equal(Vector3.One, new Vector3(1, 1, 1));
    }

    // Vector4 测试
    [Fact]
    public void Vector4_ConstructorAndProperties_WorkCorrectly()
    {
        // Arrange & Act
        Vector4 v = new Vector4(1, 2, 3, 4);

        // Assert
        Assert.Equal(1, v.X);
        Assert.Equal(2, v.Y);
        Assert.Equal(3, v.Z);
        Assert.Equal(4, v.W);
        Assert.Equal(Math.Sqrt(30), v.Magnitude, 1e-10);
        Assert.Equal(30, v.SqrMagnitude);
    }

    [Fact]
    public void Vector4_ArithmeticOperations_WorkCorrectly()
    {
        // Arrange
        Vector4 v1 = new Vector4(1, 2, 3, 4);
        Vector4 v2 = new Vector4(5, 6, 7, 8);

        // Act & Assert
        Vector4 sum = v1 + v2;
        Assert.Equal(6, sum.X);
        Assert.Equal(8, sum.Y);
        Assert.Equal(10, sum.Z);
        Assert.Equal(12, sum.W);

        Vector4 product = v1 * 2;
        Assert.Equal(2, product.X);
        Assert.Equal(4, product.Y);
        Assert.Equal(6, product.Z);
        Assert.Equal(8, product.W);
    }

    // VectorN 测试
    [Fact]
    public void VectorN_ConstructorAndProperties_WorkCorrectly()
    {
        // Arrange & Act
        VectorN v = new VectorN(1, 2, 3, 4, 5);

        // Assert
        Assert.Equal(5, v.Dimension);
        Assert.Equal(1, v[0]);
        Assert.Equal(2, v[1]);
        Assert.Equal(3, v[2]);
        Assert.Equal(4, v[3]);
        Assert.Equal(5, v[4]);
        Assert.Equal(Math.Sqrt(55), v.Magnitude, 1e-10);
        Assert.Equal(55, v.SqrMagnitude);
    }

    [Fact]
    public void VectorN_Indexer_WorksCorrectly()
    {
        // Arrange
        VectorN v = new VectorN(1, 2, 3)
        {
            // Act
            [1] = 10
        };

        // Assert
        Assert.Equal(1, v[0]);
        Assert.Equal(10, v[1]);
        Assert.Equal(3, v[2]);
    }

    [Fact]
    public void VectorN_ArithmeticOperations_WorkCorrectly()
    {
        // Arrange
        VectorN v1 = new VectorN(1, 2, 3);
        VectorN v2 = new VectorN(4, 5, 6);

        // Act & Assert
        VectorN sum = v1 + v2;
        Assert.Equal(5, sum[0]);
        Assert.Equal(7, sum[1]);
        Assert.Equal(9, sum[2]);

        VectorN difference = v1 - v2;
        Assert.Equal(-3, difference[0]);
        Assert.Equal(-3, difference[1]);
        Assert.Equal(-3, difference[2]);

        VectorN product = v1 * 2;
        Assert.Equal(2, product[0]);
        Assert.Equal(4, product[1]);
        Assert.Equal(6, product[2]);

        VectorN quotient = v2 / 2;
        Assert.Equal(2, quotient[0]);
        Assert.Equal(2.5, quotient[1]);
        Assert.Equal(3, quotient[2]);
    }

    [Fact]
    public void VectorN_DotProduct_WorksCorrectly()
    {
        // Arrange
        VectorN v1 = new VectorN(1, 2, 3);
        VectorN v2 = new VectorN(4, 5, 6);

        // Act
        double dot = v1.Dot(v2);

        // Assert
        Assert.Equal(1 * 4 + 2 * 5 + 3 * 6, dot);
    }

    [Fact]
    public void VectorN_Normalization_WorksCorrectly()
    {
        // Arrange
        VectorN v = new VectorN(3, 4);

        // Act
        VectorN normalized = v.Normalized;

        // Assert
        Assert.Equal(1, normalized.Magnitude, 1e-10);
        Assert.Equal(0.6, normalized[0], 1e-10);
        Assert.Equal(0.8, normalized[1], 1e-10);
    }

    [Fact]
    public void VectorN_ToArray_WorksCorrectly()
    {
        // Arrange
        double[] components = [1, 2, 3, 4, 5];
        VectorN v = new VectorN(components);

        // Act
        double[] result = v.ToArray();

        // Assert
        Assert.Equal(components, result);
        // 验证返回的是副本，修改不影响原向量
        result[0] = 100;
        Assert.Equal(1, v[0]);
    }

    [Fact]
    public void VectorN_StaticMethods_WorkCorrectly()
    {
        // Act
        VectorN zero = VectorN.Zero(3);
        VectorN unit1 = VectorN.Unit(3, 1);

        // Assert
        Assert.Equal(0, zero[0]);
        Assert.Equal(0, zero[1]);
        Assert.Equal(0, zero[2]);

        Assert.Equal(0, unit1[0]);
        Assert.Equal(1, unit1[1]);
        Assert.Equal(0, unit1[2]);
    }

    [Fact]
    public void VectorN_Equality_WorksCorrectly()
    {
        // Arrange
        VectorN v1 = new VectorN(1, 2, 3);
        VectorN v2 = new VectorN(1, 2, 3);
        VectorN v3 = new VectorN(4, 5, 6);

        // Act & Assert
        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.True(v1 != v3);
        Assert.False(v1 != v2);
        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void VectorN_LerpAndProject_WorkCorrectly()
    {
        // Arrange
        VectorN v1 = new VectorN(0, 0);
        VectorN v2 = new VectorN(10, 10);
        VectorN v3 = new VectorN(3, 4);

        // Act & Assert
        VectorN lerp = v1.Lerp(v2, 0.5);
        Assert.Equal(5, lerp[0]);
        Assert.Equal(5, lerp[1]);

        VectorN unitX = VectorN.Unit(2, 0);
        VectorN project = v3.Project(unitX);
        Assert.Equal(3, project[0]);
        Assert.Equal(0, project[1]);
    }

    [Fact]
    public void VectorN_Reflect_WorksCorrectly()
    {
        // Arrange
        VectorN v = new VectorN(1, -1); // 从右下方向撞击
        VectorN normal = new VectorN(0, 1); // 垂直向上的法线

        // Act
        VectorN reflected = v.Reflect(normal);

        // Assert
        Assert.Equal(1, reflected[0], 1e-10);
        Assert.Equal(1, reflected[1], 1e-10);
    }

    [Fact]
    public void VectorN_Distance_WorksCorrectly()
    {
        // Arrange
        VectorN v1 = new VectorN(0, 0, 0);
        VectorN v2 = new VectorN(1, 1, 1);

        // Act
        double distance = v1.Distance(v2);

        // Assert
        Assert.Equal(Math.Sqrt(3), distance, 1e-10);
    }

    [Fact]
    public void VectorN_Angle_WorksCorrectly()
    {
        // Arrange
        VectorN v1 = new VectorN(1, 0, 0);
        VectorN v2 = new VectorN(0, 1, 0);

        // Act
        double angle = v1.Angle(v2);

        // Assert
        Assert.Equal(Math.PI / 2, angle, 1e-10);
    }
}